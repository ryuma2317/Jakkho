using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace DIYVR
{
    /// <summary>
    /// JAKKHO Wireless VR Live Streamer.
    /// Captures the Unity VR / Player Camera output and broadcasts real-time video frames
    /// over Wi-Fi / LAN with zero wires.
    /// 
    /// Features:
    /// 1. Built-in HTTP MJPEG Server (http://0.0.0.0:8085/live.mjpg) - accessible by any browser/phone on Wi-Fi.
    /// 2. WebSocket Streamer (ws://127.0.0.1:8082) for JAKKHO Web Platform integration.
    /// 3. Zero external DLL dependencies - uses standard .NET 2.1 & Unity Engine APIs.
    /// </summary>
    public class UnityLiveWebStreamer : MonoBehaviour
    {
        [Header("Target Camera (Optional)")]
        [Tooltip("Leave empty to auto-detect Main Camera")]
        public Camera targetCamera;

        [Header("HTTP MJPEG Wi-Fi Server")]
        [Tooltip("Enable built-in HTTP MJPEG server for browser & mobile live viewing")]
        public bool enableHttpMjpegServer = true;

        [Tooltip("Port for the local HTTP MJPEG server (auto-picks the next free port if busy)")]
        public int httpPort = 8095;

        [Header("WebSocket Server (Optional)")]
        [Tooltip("Enable WebSocket streaming to external JAKKHO Web Platform server")]
        public bool enableWebSocketStreaming = false;

        [Tooltip("Target WebSocket URL")]
        public string webSocketUrl = "ws://127.0.0.1:8082/stream/unity_pc";

        [Header("Stream Quality Settings")]
        [Range(15, 60)]
        [Tooltip("Target frames per second for stream")]
        public int targetFps = 60;

        [Range(360, 1080)]
        [Tooltip("Stream vertical resolution (height in pixels)")]
        public int streamHeight = 1080;

        [Range(20, 95)]
        [Tooltip("JPEG compression quality (lower = lower latency, higher = crisper)")]
        public int jpgQuality = 90;

        [Header("Browser Auto-Launch")]
        [Tooltip("Automatically open your web browser to the casting screen when Unity enters Play mode")]
        public bool autoOpenBrowserOnPlay = false;

        [Tooltip("Web port where the JAKKHO Webcasting Platform is hosted (default: 5173 or 3000)")]
        public int webPlatformPort = 5173;

        [Header("Live Diagnostics")]
        public bool isMjpegServerRunning = false;
        public int activeHttpClients = 0;
        public int liveFps = 0;
        public long totalFramesBroadcast = 0;
        public string localStreamUrl = "";
        public string lanStreamUrl = "";

        private Camera _camera;
        private Camera _castCamera;
        private RenderTexture _renderTexture;
        private Texture2D _frameTexture;
        private float _lastFrameTime = 0f;
        private int _fpsCounter = 0;
        private float _fpsTimer = 0f;

        // HTTP MJPEG Server state
        private HttpListener _httpListener;
        private Thread _listenerThread;
        private bool _isListening = false;
        private readonly List<StreamClient> _httpClients = new List<StreamClient>();
        private readonly object _clientsLock = new object();
        private byte[] _latestJpegFrame;
        private readonly object _frameLock = new object();

        // WebSocket state
        private ClientWebSocket _webSocket;
        private CancellationTokenSource _wsCts;
        private bool _isWsSending = false;

        private class StreamClient
        {
            public HttpListenerContext Context;
            public Stream OutputStream;
            public bool IsAlive = true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoAttachToMainCamera()
        {
#if UNITY_2023_1_OR_NEWER || UNITY_6000_0_OR_NEWER
            UnityLiveWebStreamer existing = FindAnyObjectByType<UnityLiveWebStreamer>();
#else
            UnityLiveWebStreamer existing = FindObjectOfType<UnityLiveWebStreamer>();
#endif
            if (existing == null)
            {
                GameObject streamerGO = new GameObject("JAKKHO_WebCaster");
                streamerGO.AddComponent<UnityLiveWebStreamer>();
                DontDestroyOnLoad(streamerGO);
                Debug.Log("<color=#00f5d4>[JAKKHO Streamer] Spawned persistent JAKKHO_WebCaster (survives scene changes).</color>");
            }
        }

        private void Awake()
        {
            FindTargetCamera();

#if UNITY_ANDROID && !UNITY_EDITOR
            // On standalone headsets (Quest) JPEG encoding is CPU expensive in VR:
            // keep the stream light so it does not hurt the framerate.
            if (targetFps > 30) targetFps = 30;
            if (streamHeight > 720) streamHeight = 720;
#endif
        }

        private void OnEnable()
        {
            // Note: capture happens in LateUpdate. Calling Camera.Render() from inside the
            // SRP render callbacks is not allowed ("recursive rendering") so we intentionally
            // do NOT subscribe to RenderPipelineManager here.
        }

        private void OnDisable()
        {
        }

        private void FindTargetCamera()
        {
            if (targetCamera != null)
            {
                _camera = targetCamera;
            }
            else
            {
                _camera = GetComponent<Camera>() ?? GetComponentInChildren<Camera>() ?? Camera.main;
                if (_camera == null)
                {
#if UNITY_2023_1_OR_NEWER || UNITY_6000_0_OR_NEWER
                    Camera[] cams = FindObjectsByType<Camera>(FindObjectsSortMode.None);
#else
                    Camera[] cams = FindObjectsOfType<Camera>();
#endif
                    if (cams.Length > 0) _camera = cams[0];
                }
            }
        }

        private void Start()
        {
            // Optional override so the destination server can be changed without editing code:
            // use "JAKKHO VR > Set WebSocket Server from Clipboard" in the editor.
            string overrideUrl = PlayerPrefs.GetString("JAKKHO_WS_URL", "");
            if (!string.IsNullOrEmpty(overrideUrl))
            {
                webSocketUrl = overrideUrl;
                enableWebSocketStreaming = true;
                Debug.Log($"<color=#00f5d4>[JAKKHO Streamer] Using saved WebSocket server: {webSocketUrl}</color>");
            }

            FindTargetCamera();
            InitializeTextures();
            CalculateUrls();

            if (enableHttpMjpegServer)
            {
                StartHttpServer();
            }

            if (enableWebSocketStreaming)
            {
                StartCoroutine(WebSocketLoopCoroutine());
            }

            if (autoOpenBrowserOnPlay)
            {
                string targetWebUrl = $"http://localhost:{webPlatformPort}/cast";
                Application.OpenURL(targetWebUrl);
                Debug.Log($"<color=#00f5d4>[JAKKHO Streamer] Auto-opened browser cast at {targetWebUrl}</color>");
            }

            Debug.Log($"<color=#00f5d4>[JAKKHO Streamer] Initialized! Stream URLs:\n- Localhost: {localStreamUrl}\n- LAN (Wi-Fi): {lanStreamUrl}</color>");
        }

#if UNITY_EDITOR
        [UnityEditor.MenuItem("JAKKHO VR/Open Webcast in Browser (5173)", false, 1)]
        public static void OpenWebcastBrowser5173()
        {
            Application.OpenURL("http://localhost:5173/cast");
        }

        [UnityEditor.MenuItem("JAKKHO VR/Open Webcast in Browser (3000)", false, 2)]
        public static void OpenWebcastBrowser3000()
        {
            Application.OpenURL("http://localhost:3000/cast");
        }

        [UnityEditor.MenuItem("JAKKHO VR/Open Direct MJPEG Stream (8095)", false, 3)]
        public static void OpenDirectMjpegStream()
        {
            Application.OpenURL("http://localhost:8095/live.mjpg");
        }

        [UnityEditor.MenuItem("JAKKHO VR/Set WebSocket Server from Clipboard", false, 4)]
        public static void SetWebSocketServerFromClipboard()
        {
            string clip = UnityEditor.EditorGUIUtility.systemCopyBuffer;
            if (!string.IsNullOrEmpty(clip) && clip.Trim().StartsWith("ws"))
            {
                PlayerPrefs.SetString("JAKKHO_WS_URL", clip.Trim());
                PlayerPrefs.Save();
                Debug.Log($"<color=#00f5d4>[JAKKHO Streamer] WebSocket server saved: {clip.Trim()} (streaming enabled on Play).</color>");
            }
            else
            {
                Debug.LogWarning($"[JAKKHO Streamer] Clipboard does not contain a ws:// URL. Found: '{clip}'");
            }
        }

        [UnityEditor.MenuItem("JAKKHO VR/Clear Saved WebSocket Server", false, 5)]
        public static void ClearSavedWebSocketServer()
        {
            PlayerPrefs.DeleteKey("JAKKHO_WS_URL");
            PlayerPrefs.Save();
            Debug.Log("<color=#00f5d4>[JAKKHO Streamer] Saved WebSocket server cleared.</color>");
        }
#endif

        private void CalculateUrls()
        {
            localStreamUrl = $"http://localhost:{httpPort}/live.mjpg";
            string lanIp = GetLocalIPAddress();
            lanStreamUrl = $"http://{lanIp}:{httpPort}/live.mjpg";
        }

        private string GetLocalIPAddress()
        {
            try
            {
                string fallback = null;

                foreach (var ni in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus != System.Net.NetworkInformation.OperationalStatus.Up) continue;
                    if (ni.NetworkInterfaceType == System.Net.NetworkInformation.NetworkInterfaceType.Loopback) continue;

                    string name = (ni.Name + " " + ni.Description).ToLowerInvariant();
                    bool virtualAdapter = name.Contains("vmware") || name.Contains("virtualbox") || name.Contains("hyper-v")
                        || name.Contains("vethernet") || name.Contains("tailscale") || name.Contains("zerotier")
                        || name.Contains("loopback") || name.Contains("bluetooth");

                    foreach (var ua in ni.GetIPProperties().UnicastAddresses)
                    {
                        if (ua.Address.AddressFamily != AddressFamily.InterNetwork) continue;
                        string ip = ua.Address.ToString();
                        if (IPAddress.IsLoopback(ua.Address)) continue;

                        bool isLinkLocal = ip.StartsWith("169.254.");

                        bool isPrivate = ip.StartsWith("192.168.") || ip.StartsWith("10.");
                        if (!isPrivate && ip.StartsWith("172."))
                        {
                            int second;
                            var parts = ip.Split('.');
                            if (parts.Length > 1 && int.TryParse(parts[1], out second) && second >= 16 && second <= 31)
                                isPrivate = true;
                        }

                        // Best: real (non-virtual) private LAN address, e.g. Wi-Fi / Ethernet.
                        if (isPrivate && !virtualAdapter && !isLinkLocal)
                            return ip;

                        if (!isLinkLocal && fallback == null)
                            fallback = ip;
                    }
                }

                if (fallback != null) return fallback;
            }
            catch { }
            return "127.0.0.1";
        }

        private void InitializeTextures()
        {
            int streamWidth = Mathf.RoundToInt(streamHeight * (16f / 9f));
            _renderTexture = new RenderTexture(streamWidth, streamHeight, 24, RenderTextureFormat.ARGB32)
            {
                antiAliasing = 4,
                filterMode = FilterMode.Bilinear
            };
            _frameTexture = new Texture2D(streamWidth, streamHeight, TextureFormat.RGB24, false);
            CreateCastCamera();
        }

        /// <summary>
        /// Creates a dedicated mono "cast" camera parented to the main/XR camera.
        /// It renders one eye view into the stream RenderTexture every frame without
        /// disturbing the VR stereo rendering (no manual Camera.Render on the XR camera).
        /// </summary>
        private void CreateCastCamera()
        {
            if (_camera == null || _renderTexture == null) return;

            if (_castCamera != null)
            {
                Destroy(_castCamera.gameObject);
                _castCamera = null;
            }

            var castGO = new GameObject("JAKKHO_CastCamera");
            castGO.transform.SetParent(_camera.transform, false);
            _castCamera = castGO.AddComponent<Camera>();
            _castCamera.CopyFrom(_camera);
            _castCamera.stereoTargetEye = StereoTargetEyeMask.None;
            _castCamera.targetTexture = _renderTexture;
            _castCamera.depth = _camera.depth - 10f;
            _castCamera.allowHDR = false;
            _castCamera.allowMSAA = false;

            // URP cameras need additional camera data; add it when the package is present.
            var urpType = Type.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");
            if (urpType != null && castGO.GetComponent(urpType) == null)
                castGO.AddComponent(urpType);
        }

        #region HTTP MJPEG Server

        private void StartHttpServer()
        {
            // Try the configured port first, then the next 9 ports if it is occupied.
            for (int port = httpPort; port < httpPort + 10; port++)
            {
                if (!IsPortFree(port))
                {
                    Debug.LogWarning($"[JAKKHO Streamer] Port {port} is occupied, trying next...");
                    continue;
                }

                if (TryStartHttpServer(port))
                {
                    if (port != httpPort)
                    {
                        Debug.LogWarning($"[JAKKHO Streamer] Port {httpPort} was busy - switched to {port}.");
                        httpPort = port;
                        CalculateUrls();
                    }
                    return;
                }
            }

            Debug.LogError($"[JAKKHO Streamer] Could not start HTTP server on any port {httpPort}-{httpPort + 9}.");
            isMjpegServerRunning = false;
        }

        private static bool IsPortFree(int port)
        {
            try
            {
                var probe = new TcpListener(IPAddress.Loopback, port);
                probe.Start();
                probe.Stop();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool TryStartHttpServer(int port)
        {
            try
            {
                _httpListener = new HttpListener();
                // Listen on all network interfaces
                _httpListener.Prefixes.Add($"http://*:{port}/");
                _httpListener.Start();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[JAKKHO Streamer] Wildcard bind failed on {port} ({ex.Message}). Falling back to localhost prefix...");
                try
                {
                    _httpListener = new HttpListener();
                    _httpListener.Prefixes.Add($"http://localhost:{port}/");
                    _httpListener.Prefixes.Add($"http://127.0.0.1:{port}/");
                    _httpListener.Start();
                }
                catch (Exception fallbackEx)
                {
                    Debug.LogWarning($"[JAKKHO Streamer] Port {port} unavailable ({fallbackEx.Message}).");
                    try { _httpListener?.Close(); } catch { }
                    _httpListener = null;
                    return false;
                }
            }

            _isListening = true;
            isMjpegServerRunning = true;
            _listenerThread = new Thread(ListenLoop)
            {
                IsBackground = true,
                Name = "JAKKHO_MJPEG_Listener"
            };
            _listenerThread.Start();
            Debug.Log($"<color=#00f5d4>[JAKKHO Streamer] HTTP MJPEG Server started on port {port}</color>");
            return true;
        }

        private void ListenLoop()
        {
            while (_isListening && _httpListener != null && _httpListener.IsListening)
            {
                try
                {
                    HttpListenerContext context = _httpListener.GetContext();
                    ThreadPool.QueueUserWorkItem(ProcessClientRequest, context);
                }
                catch (HttpListenerException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    if (_isListening)
                    {
                        Debug.LogWarning($"[JAKKHO Streamer] Listener error: {ex.Message}");
                    }
                }
            }
        }

        private void ProcessClientRequest(object state)
        {
            HttpListenerContext context = (HttpListenerContext)state;
            string rawUrl = context.Request.RawUrl.ToLower();

            // Handle CORS headers
            context.Response.AddHeader("Access-Control-Allow-Origin", "*");
            context.Response.AddHeader("Access-Control-Allow-Methods", "GET, OPTIONS");
            context.Response.AddHeader("Access-Control-Allow-Headers", "*");
            context.Response.AddHeader("Cross-Origin-Resource-Policy", "cross-origin");

            if (context.Request.HttpMethod == "OPTIONS")
            {
                context.Response.StatusCode = 200;
                context.Response.Close();
                return;
            }

            if (rawUrl.Contains("live.mjpg") || rawUrl.Contains("stream") || rawUrl.Contains("video"))
            {
                // Serve continuous MJPEG stream
                context.Response.ContentType = "multipart/x-mixed-replace; boundary=--jakkho_frame";
                context.Response.StatusCode = 200;
                context.Response.SendChunked = true;

                StreamClient client = new StreamClient
                {
                    Context = context,
                    OutputStream = context.Response.OutputStream,
                    IsAlive = true
                };

                lock (_clientsLock)
                {
                    _httpClients.Add(client);
                    activeHttpClients = _httpClients.Count;
                }

                Debug.Log($"[JAKKHO Streamer] Client connected to live stream. Total clients: {activeHttpClients}");
            }
            else if (rawUrl.Contains("snapshot.jpg") || rawUrl.Contains("frame.jpg"))
            {
                // Serve single snapshot JPEG
                byte[] frame = null;
                lock (_frameLock)
                {
                    frame = _latestJpegFrame;
                }

                if (frame != null && frame.Length > 0)
                {
                    context.Response.ContentType = "image/jpeg";
                    context.Response.ContentLength64 = frame.Length;
                    context.Response.OutputStream.Write(frame, 0, frame.Length);
                }
                else
                {
                    context.Response.StatusCode = 503;
                }
                context.Response.Close();
            }
            else
            {
                // Serve landing diagnostic HTML page
                string html = $@"<!DOCTYPE html>
<html>
<head>
    <title>JAKKHO VR Live Stream</title>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        body {{ margin:0; background:#020617; color:#f8fafc; font-family:monospace; display:flex; flex-direction:column; align-items:center; justify-content:center; min-height:100vh; }}
        .frame {{ border:4px solid white; border-radius:24px; overflow:hidden; box-shadow:0 0 40px rgba(6,182,212,0.3); background:#000; max-width:90vw; }}
        img {{ width:100%; max-height:75vh; display:block; object-fit:contain; }}
        .badge {{ background:rgba(6,182,212,0.1); border:1px solid #06b6d4; color:#06b6d4; padding:6px 14px; border-radius:20px; font-weight:bold; margin-bottom:12px; font-size:12px; }}
        .stats {{ display:flex; gap:16px; margin-top:14px; font-size:12px; color:#94a3b8; }}
        .val {{ color:#38bdf8; font-weight:bold; }}
    </style>
</head>
<body>
    <div class='badge'>◆ JAKKHO WIRELESS VR STREAM</div>
    <div class='frame'>
        <img src='/live.mjpg' alt='JAKKHO VR Live Viewport' />
    </div>
    <div class='stats'>
        <div>FPS: <span class='val'>{liveFps}</span></div>
        <div>Resolution: <span class='val'>{streamHeight * 16 / 9}x{streamHeight}</span></div>
        <div>Clients: <span class='val'>{activeHttpClients}</span></div>
        <div>Frames: <span class='val'>{totalFramesBroadcast}</span></div>
    </div>
</body>
</html>";
                byte[] htmlBytes = Encoding.UTF8.GetBytes(html);
                context.Response.ContentType = "text/html; charset=utf-8";
                context.Response.ContentLength64 = htmlBytes.Length;
                context.Response.OutputStream.Write(htmlBytes, 0, htmlBytes.Length);
                context.Response.Close();
            }
        }

        private void BroadcastFrameToHttpClients(byte[] jpegData)
        {
            if (jpegData == null || jpegData.Length == 0) return;

            string header = $"--jakkho_frame\r\nContent-Type: image/jpeg\r\nContent-Length: {jpegData.Length}\r\n\r\n";
            byte[] headerBytes = Encoding.ASCII.GetBytes(header);
            byte[] footerBytes = Encoding.ASCII.GetBytes("\r\n");

            List<StreamClient> deadClients = null;

            lock (_clientsLock)
            {
                if (_httpClients.Count == 0) return;

                for (int i = 0; i < _httpClients.Count; i++)
                {
                    StreamClient client = _httpClients[i];
                    try
                    {
                        client.OutputStream.Write(headerBytes, 0, headerBytes.Length);
                        client.OutputStream.Write(jpegData, 0, jpegData.Length);
                        client.OutputStream.Write(footerBytes, 0, footerBytes.Length);
                        client.OutputStream.Flush();
                    }
                    catch
                    {
                        client.IsAlive = false;
                        if (deadClients == null) deadClients = new List<StreamClient>();
                        deadClients.Add(client);
                    }
                }

                if (deadClients != null)
                {
                    foreach (var dead in deadClients)
                    {
                        _httpClients.Remove(dead);
                        try { dead.Context.Response.Close(); } catch { }
                    }
                    activeHttpClients = _httpClients.Count;
                }
            }
        }

        #endregion

        #region Frame Capture

        private void EnsureResources()
        {
            if (_camera == null)
            {
                FindTargetCamera();
            }

            if (_renderTexture == null || _frameTexture == null)
            {
                InitializeTextures();
            }

            // Recreate the cast camera if the main camera changed (e.g. after a scene load).
            if (_camera != null && (_castCamera == null || _castCamera.transform.parent != _camera.transform))
            {
                CreateCastCamera();
            }
        }

        private void LateUpdate()
        {
            _fpsTimer += Time.deltaTime;
            if (_fpsTimer >= 1.0f)
            {
                liveFps = _fpsCounter;
                _fpsCounter = 0;
                _fpsTimer = 0f;
            }

            // Capture outside of the render pipeline callbacks for BOTH built-in and SRP,
            // so Unity never reports recursive rendering / recursive PlayerLoop.
            float frameInterval = 1f / targetFps;
            if (Time.time - _lastFrameTime >= frameInterval)
            {
                _lastFrameTime = Time.time;
                CaptureFrame();
            }
        }

        private void CaptureFrame()
        {
            EnsureResources();
            if (_camera == null || _renderTexture == null || _frameTexture == null) return;

            // Preferred path: the dedicated cast camera already rendered this frame into
            // the RenderTexture (works in VR / mono / flatscreen without extra renders).
            if (_castCamera != null && _castCamera.targetTexture == _renderTexture && _castCamera.isActiveAndEnabled)
            {
                RenderTexture currentRT2 = RenderTexture.active;
                RenderTexture.active = _renderTexture;
                _frameTexture.ReadPixels(new Rect(0, 0, _renderTexture.width, _renderTexture.height), 0, 0);
                _frameTexture.Apply();
                RenderTexture.active = currentRT2;
                ProcessEncodedFrame();
                return;
            }

            RenderTexture currentRT = RenderTexture.active;
            RenderTexture.active = _renderTexture;

            RenderTexture prevTarget = _camera.targetTexture;
            _camera.targetTexture = _renderTexture;
            _camera.Render();
            _camera.targetTexture = prevTarget;

            _frameTexture.ReadPixels(new Rect(0, 0, _renderTexture.width, _renderTexture.height), 0, 0);
            _frameTexture.Apply();

            RenderTexture.active = currentRT;

            ProcessEncodedFrame();
        }

        private void ProcessEncodedFrame()
        {
            byte[] jpgBytes = _frameTexture.EncodeToJPG(jpgQuality);
            if (jpgBytes != null && jpgBytes.Length > 0)
            {
                _fpsCounter++;
                totalFramesBroadcast++;

                lock (_frameLock)
                {
                    _latestJpegFrame = jpgBytes;
                }

                if (_httpClients.Count > 0)
                {
                    ThreadPool.QueueUserWorkItem(_ => BroadcastFrameToHttpClients(jpgBytes));
                }

                if (enableWebSocketStreaming && _webSocket != null && _webSocket.State == WebSocketState.Open && !_isWsSending)
                {
                    _isWsSending = true;
                    Task.Run(() => SendWebSocketFrameAsync(jpgBytes));
                }
            }
        }

        #endregion

        #region WebSocket Streamer

        private IEnumerator WebSocketLoopCoroutine()
        {
            while (enableWebSocketStreaming)
            {
                if (_webSocket == null || _webSocket.State != WebSocketState.Open)
                {
                    yield return StartCoroutine(ConnectWebSocket());
                }
                yield return new WaitForSeconds(3f);
            }
        }

        private IEnumerator ConnectWebSocket()
        {
            _wsCts = new CancellationTokenSource();
            _webSocket = new ClientWebSocket();

            Task connectTask = null;
            try
            {
                Uri uri = new Uri(webSocketUrl);
                connectTask = _webSocket.ConnectAsync(uri, _wsCts.Token);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[JAKKHO Streamer] Invalid WebSocket URL: {ex.Message}");
                yield break;
            }

            while (!connectTask.IsCompleted)
            {
                yield return null;
            }

            if (_webSocket.State == WebSocketState.Open)
            {
                Debug.Log($"<color=#00f5d4>[JAKKHO Streamer] WebSocket Connected to {webSocketUrl}!</color>");
            }
        }

        private async Task SendWebSocketFrameAsync(byte[] frameData)
        {
            try
            {
                if (_webSocket != null && _webSocket.State == WebSocketState.Open)
                {
                    byte[] packet = new byte[9 + frameData.Length];
                    packet[0] = 0x01; // JPEG Type
                    long pts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    byte[] ptsBytes = BitConverter.GetBytes(pts);
                    Array.Copy(ptsBytes, 0, packet, 1, 8);
                    Array.Copy(frameData, 0, packet, 9, frameData.Length);

                    ArraySegment<byte> buffer = new ArraySegment<byte>(packet);
                    await _webSocket.SendAsync(buffer, WebSocketMessageType.Binary, true, _wsCts.Token);
                }
            }
            catch
            {
                // Soft fail, will reconnect
            }
            finally
            {
                _isWsSending = false;
            }
        }

        #endregion

        private void OnDestroy()
        {
            _isListening = false;
            if (_httpListener != null)
            {
                try
                {
                    _httpListener.Stop();
                    _httpListener.Close();
                }
                catch { }
                _httpListener = null;
            }

            lock (_clientsLock)
            {
                foreach (var client in _httpClients)
                {
                    try { client.Context.Response.Close(); } catch { }
                }
                _httpClients.Clear();
            }

            _wsCts?.Cancel();
            if (_webSocket != null)
            {
                try { _webSocket.Dispose(); } catch { }
                _webSocket = null;
            }

            if (_renderTexture != null)
            {
                _renderTexture.Release();
                Destroy(_renderTexture);
            }

            if (_castCamera != null)
            {
                Destroy(_castCamera.gameObject);
                _castCamera = null;
            }

            if (_frameTexture != null)
            {
                Destroy(_frameTexture);
            }
        }

        private void OnApplicationQuit()
        {
            OnDestroy();
        }
    }
}
