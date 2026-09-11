#if UNITY_EDITOR
using UnityEngine;

/// <summary>
/// Procedurally draws crisp, flat vector-style icon sprites (Telegram / Chrome / UI)
/// into RGBA textures at build time. Everything is rendered with 4x supersampling
/// for clean edges, then downsampled. Sprites are cached statically.
/// </summary>
public static class ProceduralIconSet
{
    public enum Icon
    {
        MenuHamburger,      // Telegram sidebar
        Search,             // magnifier
        Phone,              // handset (call)
        MoreDots,           // "..." header tools
        Paperclip,          // attach
        Emoji,              // smiley face
        Mic,                // voice message
        SendPlane,          // paper plane (Telegram send)
        VideoCall,          // camera
        Minimize,           // window
        Maximize,           // window
        Close,              // window X
        BackArrow,          // browser back
        ForwardArrow,       // browser forward
        Reload,             // circular arrow
        LockClosed,         // secure lock
        Puzzle,             // extensions piece
        Star,               // bookmark/star
        Profile,            // person circle
        WarningTriangle,    // not-secure / warning
        Info,               // info circle
        Shield,             // 2FA / defense
        QrScan,             // camera frame for QR
        Check,              // single check
        DoubleCheck,        // read receipts
        Plus,
        Globe,
        Home,
        Eye,
        Trash,
        Circle,
    }

    const int BaseSize = 128;
    const int SS = 4; // supersample factor
    const int FullSize = BaseSize * SS;

    static readonly Sprite[] s_Cache = new Sprite[System.Enum.GetValues(typeof(Icon)).Length];

    public static Sprite Get(Icon icon)
    {
        int idx = (int)icon;
        if (s_Cache[idx] != null) return s_Cache[idx];
        s_Cache[idx] = CreateIconSprite(icon);
        return s_Cache[idx];
    }

    static Sprite CreateIconSprite(Icon icon)
    {
        Color[] px = new Color[FullSize * FullSize];
        for (int i = 0; i < px.Length; i++) px[i] = Color.clear;

        switch (icon)
        {
            case Icon.MenuHamburger:
                DrawRoundedLine(px, 0.30f, 0.42f, 0.70f, 0.42f, 0.055f);
                DrawRoundedLine(px, 0.30f, 0.58f, 0.70f, 0.58f, 0.055f);
                DrawRoundedLine(px, 0.30f, 0.74f, 0.70f, 0.74f, 0.055f);
                break;
            case Icon.Search:
                DrawRing(px, 0.43f, 0.43f, 0.20f, 0.085f);
                DrawRoundedLine(px, 0.57f, 0.57f, 0.83f, 0.83f, 0.09f);
                break;
            case Icon.Phone:
                DrawPhone(px);
                break;
            case Icon.MoreDots:
                DrawCircle(px, 0.32f, 0.55f, 0.075f);
                DrawCircle(px, 0.50f, 0.55f, 0.075f);
                DrawCircle(px, 0.68f, 0.55f, 0.075f);
                break;
            case Icon.Paperclip:
                DrawPaperclip(px);
                break;
            case Icon.Emoji:
                DrawCircle(px, 0.50f, 0.52f, 0.33f);
                DrawCircle(px, 0.38f, 0.44f, 0.06f);
                DrawCircle(px, 0.62f, 0.44f, 0.06f);
                DrawArc(px, 0.50f, 0.50f, 0.17f, 20f, 160f, 0.06f);
                break;
            case Icon.Mic:
                DrawMic(px);
                break;
            case Icon.SendPlane:
                DrawPaperPlane(px);
                break;
            case Icon.VideoCall:
                DrawVideo(px);
                break;
            case Icon.Minimize:
                DrawRoundedLine(px, 0.26f, 0.72f, 0.74f, 0.72f, 0.07f);
                break;
            case Icon.Maximize:
                DrawSquare(px, 0.30f, 0.30f, 0.70f, 0.70f, 0.07f);
                break;
            case Icon.Close:
                DrawRoundedLine(px, 0.30f, 0.30f, 0.70f, 0.70f, 0.07f);
                DrawRoundedLine(px, 0.70f, 0.30f, 0.30f, 0.70f, 0.07f);
                break;
            case Icon.BackArrow:
                DrawArrowLeft(px);
                break;
            case Icon.ForwardArrow:
                DrawArrowRight(px);
                break;
            case Icon.Reload:
                DrawReload(px);
                break;
            case Icon.LockClosed:
                DrawLock(px);
                break;
            case Icon.Puzzle:
                DrawPuzzle(px);
                break;
            case Icon.Star:
                DrawStar(px);
                break;
            case Icon.Profile:
                DrawCircle(px, 0.50f, 0.38f, 0.17f);
                DrawHalfDisc(px, 0.50f, 0.78f, 0.30f);
                break;
            case Icon.WarningTriangle:
                DrawWarning(px);
                break;
            case Icon.Info:
                DrawRing(px, 0.50f, 0.50f, 0.33f, 0.07f);
                DrawCircle(px, 0.50f, 0.40f, 0.055f);
                DrawRoundedLine(px, 0.50f, 0.50f, 0.50f, 0.66f, 0.055f);
                break;
            case Icon.Shield:
                DrawShield(px);
                break;
            case Icon.QrScan:
                DrawQrScan(px);
                break;
            case Icon.Check:
                DrawRoundedLine(px, 0.26f, 0.52f, 0.44f, 0.70f, 0.085f);
                DrawRoundedLine(px, 0.44f, 0.70f, 0.76f, 0.32f, 0.085f);
                break;
            case Icon.DoubleCheck:
                DrawRoundedLine(px, 0.14f, 0.55f, 0.32f, 0.73f, 0.07f);
                DrawRoundedLine(px, 0.32f, 0.73f, 0.62f, 0.37f, 0.07f);
                DrawRoundedLine(px, 0.36f, 0.55f, 0.54f, 0.73f, 0.07f);
                DrawRoundedLine(px, 0.54f, 0.73f, 0.84f, 0.37f, 0.07f);
                break;
            case Icon.Plus:
                DrawRoundedLine(px, 0.50f, 0.28f, 0.50f, 0.72f, 0.07f);
                DrawRoundedLine(px, 0.28f, 0.50f, 0.72f, 0.50f, 0.07f);
                break;
            case Icon.Globe:
                DrawRing(px, 0.50f, 0.50f, 0.33f, 0.07f);
                DrawArc(px, 0.50f, 0.50f, 0.33f, 0f, 360f, 0.07f, true);
                DrawRoundedLine(px, 0.18f, 0.50f, 0.82f, 0.50f, 0.055f);
                DrawArc(px, 0.50f, 0.50f, 0.12f, 90f, 270f, 0.055f, true);
                DrawArc(px, 0.50f, 0.50f, 0.12f, -90f, 90f, 0.055f, true);
                break;
            case Icon.Home:
                DrawPoly(px, new Vector2[] { new Vector2(0.50f, 0.18f), new Vector2(0.84f, 0.52f), new Vector2(0.72f, 0.52f), new Vector2(0.72f, 0.82f), new Vector2(0.28f, 0.82f), new Vector2(0.28f, 0.52f), new Vector2(0.16f, 0.52f) });
                break;
            case Icon.Eye:
                DrawEye(px);
                break;
            case Icon.Trash:
                DrawTrash(px);
                break;
            case Icon.Circle:
                DrawCircle(px, 0.50f, 0.50f, 0.50f);
                break;
        }

        Texture2D tex = new Texture2D(BaseSize, BaseSize, TextureFormat.RGBA32, false);
        for (int y = 0; y < BaseSize; y++)
        {
            for (int x = 0; x < BaseSize; x++)
            {
                Color acc = Color.clear;
                for (int sy = 0; sy < SS; sy++)
                {
                    for (int sx = 0; sx < SS; sx++)
                    {
                        acc += px[(y * SS + sy) * FullSize + (x * SS + sx)];
                    }
                }
                tex.SetPixel(x, y, acc / (SS * SS));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, BaseSize, BaseSize), new Vector2(0.5f, 0.5f), 100f);
    }

    // ─── low-level drawing helpers (normalized 0..1 coords, y up) ────────────────

    static void SetPixel(Color[] px, int x, int y, Color c)
    {
        if (x < 0 || x >= FullSize || y < 0 || y >= FullSize) return;
        int i = y * FullSize + x;
        if (c.a > px[i].a) px[i] = c;
    }

    static void DrawCircle(Color[] px, float cx, float cy, float r)
    {
        int R = Mathf.RoundToInt(r * FullSize);
        int CX = Mathf.RoundToInt(cx * FullSize);
        int CY = Mathf.RoundToInt(cy * FullSize);
        for (int y = -R; y <= R; y++)
        {
            for (int x = -R; x <= R; x++)
            {
                if (x * x + y * y <= R * R)
                    SetPixel(px, CX + x, CY + y, Color.white);
            }
        }
    }

    static void DrawRing(Color[] px, float cx, float cy, float rOuter, float thickness)
    {
        int Ro = Mathf.RoundToInt(rOuter * FullSize);
        int Ri = Mathf.RoundToInt((rOuter - thickness) * FullSize);
        int CX = Mathf.RoundToInt(cx * FullSize);
        int CY = Mathf.RoundToInt(cy * FullSize);
        for (int y = -Ro; y <= Ro; y++)
        {
            for (int x = -Ro; x <= Ro; x++)
            {
                int d2 = x * x + y * y;
                if (d2 <= Ro * Ro && d2 >= Ri * Ri)
                    SetPixel(px, CX + x, CY + y, Color.white);
            }
        }
    }

    static void DrawHalfDisc(Color[] px, float cx, float cy, float r)
    {
        int R = Mathf.RoundToInt(r * FullSize);
        int CX = Mathf.RoundToInt(cx * FullSize);
        int CY = Mathf.RoundToInt(cy * FullSize);
        for (int y = 0; y <= R; y++)
        {
            for (int x = -R; x <= R; x++)
            {
                if (x * x + y * y <= R * R)
                    SetPixel(px, CX + x, CY + y, Color.white);
            }
        }
    }

    static void DrawRoundedLine(Color[] px, float x1, float y1, float x2, float y2, float thickness)
    {
        int X1 = Mathf.RoundToInt(x1 * FullSize);
        int Y1 = Mathf.RoundToInt(y1 * FullSize);
        int X2 = Mathf.RoundToInt(x2 * FullSize);
        int Y2 = Mathf.RoundToInt(y2 * FullSize);
        int T = Mathf.RoundToInt(thickness * FullSize * 0.5f);
        int T2 = T * T;
        float len = Mathf.Sqrt((X2 - X1) * (X2 - X1) + (Y2 - Y1) * (Y2 - Y1));
        int minX = Mathf.Max(0, Mathf.Min(X1, X2) - T);
        int maxX = Mathf.Min(FullSize - 1, Mathf.Max(X1, X2) + T);
        int minY = Mathf.Max(0, Mathf.Min(Y1, Y2) - T);
        int maxY = Mathf.Min(FullSize - 1, Mathf.Max(Y1, Y2) + T);
        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                // distance from point to segment
                float ux = x - X1, uy = y - Y1;
                float vx = X2 - X1, vy = Y2 - Y1;
                float dot = ux * vx + uy * vy;
                float t = Mathf.Clamp01(dot / (len * len));
                float pxDist = Mathf.Sqrt((x - (X1 + t * vx)) * (x - (X1 + t * vx)) + (y - (Y1 + t * vy)) * (y - (Y1 + t * vy)));
                if (pxDist * pxDist <= T2)
                    SetPixel(px, x, y, Color.white);
            }
        }
    }

    static void DrawArc(Color[] px, float cx, float cy, float radius, float startDeg, float endDeg, float thickness, bool horizontal = false)
    {
        int T = Mathf.RoundToInt(thickness * FullSize * 0.5f);
        int T2 = T * T;
        int R = Mathf.RoundToInt(radius * FullSize);
        int CX = Mathf.RoundToInt(cx * FullSize);
        int CY = Mathf.RoundToInt(cy * FullSize);
        float startRad = startDeg * Mathf.Deg2Rad;
        float endRad = endDeg * Mathf.Deg2Rad;
        int minX = Mathf.Max(0, CX - R - T);
        int maxX = Mathf.Min(FullSize - 1, CX + R + T);
        int minY = Mathf.Max(0, CY - R - T);
        int maxY = Mathf.Min(FullSize - 1, CY + R + T);
        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                float dx = x - CX, dy = y - CY;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                if (Mathf.Abs(dist - R) > T) continue;
                float ang = Mathf.Atan2(dy, dx);
                if (ang < 0) ang += Mathf.PI * 2f;
                float sa = startRad < 0 ? startRad + Mathf.PI * 2f : startRad;
                float ea = endRad < 0 ? endRad + Mathf.PI * 2f : endRad;
                bool inArc = horizontal ? (ang <= sa && ang >= ea) : (ang >= sa && ang <= ea);
                if (inArc || Mathf.Abs(sa - ea) > 6f)
                    SetPixel(px, x, y, Color.white);
            }
        }
    }

    static void DrawPoly(Color[] px, Vector2[] pts)
    {
        if (pts == null || pts.Length < 3) return;
        // rasterize via point-in-polygon over bounding box
        float minX = 1f, maxX = 0f, minY = 1f, maxY = 0f;
        foreach (var p in pts)
        {
            minX = Mathf.Min(minX, p.x); maxX = Mathf.Max(maxX, p.x);
            minY = Mathf.Min(minY, p.y); maxY = Mathf.Max(maxY, p.y);
        }
        int X0 = Mathf.Max(0, Mathf.RoundToInt(minX * FullSize) - 1);
        int X1 = Mathf.Min(FullSize - 1, Mathf.RoundToInt(maxX * FullSize) + 1);
        int Y0 = Mathf.Max(0, Mathf.RoundToInt(minY * FullSize) - 1);
        int Y1 = Mathf.Min(FullSize - 1, Mathf.RoundToInt(maxY * FullSize) + 1);
        for (int y = Y0; y <= Y1; y++)
        {
            for (int x = X0; x <= X1; x++)
            {
                float pxX = (x + 0.5f) / FullSize;
                float pxY = (y + 0.5f) / FullSize;
                bool inside = false;
                int n = pts.Length;
                for (int i = 0, j = n - 1; i < n; j = i++)
                {
                    if ((pts[i].y > pxY) != (pts[j].y > pxY) &&
                        pxX < (pts[j].x - pts[i].x) * (pxY - pts[i].y) / (pts[j].y - pts[i].y) + pts[i].x)
                        inside = !inside;
                }
                if (inside) SetPixel(px, x, y, Color.white);
            }
        }
    }

    static void DrawSquare(Color[] px, float x0, float y0, float x1, float y1, float thickness)
    {
        DrawRoundedLine(px, x0, y0, x1, y0, thickness);
        DrawRoundedLine(px, x1, y0, x1, y1, thickness);
        DrawRoundedLine(px, x1, y1, x0, y1, thickness);
        DrawRoundedLine(px, x0, y1, x0, y0, thickness);
    }

    // ─── complex glyphs ───────────────────────────────────────────────────────────

    static void DrawPaperPlane(Color[] px)
    {
        DrawPoly(px, new Vector2[]
        {
            new Vector2(0.16f, 0.80f), new Vector2(0.88f, 0.28f),
            new Vector2(0.50f, 0.52f), new Vector2(0.16f, 0.80f),
        });
        DrawPoly(px, new Vector2[]
        {
            new Vector2(0.50f, 0.52f), new Vector2(0.88f, 0.28f),
            new Vector2(0.62f, 0.80f), new Vector2(0.50f, 0.52f),
        });
    }

    static void DrawPhone(Color[] px)
    {
        // handset: rounded vertical body + ear/mouth arcs
        DrawRoundedLine(px, 0.50f, 0.30f, 0.50f, 0.70f, 0.22f);
        DrawCircle(px, 0.50f, 0.74f, 0.12f);
        DrawCircle(px, 0.50f, 0.26f, 0.12f);
        DrawRing(px, 0.50f, 0.74f, 0.20f, 0.07f);
        DrawRing(px, 0.50f, 0.26f, 0.20f, 0.07f);
    }

    static void DrawPaperclip(Color[] px)
    {
        // simple clip: two arcs + stem
        DrawArc(px, 0.40f, 0.62f, 0.24f, 40f, 320f, 0.06f);
        DrawRoundedLine(px, 0.42f, 0.35f, 0.42f, 0.62f, 0.06f);
        DrawRoundedLine(px, 0.42f, 0.35f, 0.58f, 0.35f, 0.06f);
    }

    static void DrawMic(Color[] px)
    {
        DrawRoundedLine(px, 0.50f, 0.20f, 0.50f, 0.52f, 0.14f);
        DrawRing(px, 0.50f, 0.58f, 0.17f, 0.06f, false);
        DrawRoundedLine(px, 0.34f, 0.66f, 0.66f, 0.66f, 0.06f);
        DrawRoundedLine(px, 0.42f, 0.72f, 0.58f, 0.72f, 0.06f);
    }

    static void DrawVideo(Color[] px)
    {
        DrawSquare(px, 0.16f, 0.30f, 0.62f, 0.70f, 0.06f);
        DrawPoly(px, new Vector2[] { new Vector2(0.66f, 0.42f), new Vector2(0.86f, 0.30f), new Vector2(0.86f, 0.70f), new Vector2(0.66f, 0.58f) });
    }

    static void DrawArrowLeft(Color[] px)
    {
        DrawRoundedLine(px, 0.20f, 0.50f, 0.80f, 0.50f, 0.07f);
        DrawPoly(px, new Vector2[] { new Vector2(0.16f, 0.50f), new Vector2(0.42f, 0.30f), new Vector2(0.42f, 0.70f) });
    }

    static void DrawArrowRight(Color[] px)
    {
        DrawRoundedLine(px, 0.20f, 0.50f, 0.80f, 0.50f, 0.07f);
        DrawPoly(px, new Vector2[] { new Vector2(0.84f, 0.50f), new Vector2(0.58f, 0.30f), new Vector2(0.58f, 0.70f) });
    }

    static void DrawReload(Color[] px)
    {
        DrawArc(px, 0.50f, 0.50f, 0.28f, 20f, 300f, 0.07f);
        DrawPoly(px, new Vector2[] { new Vector2(0.70f, 0.28f), new Vector2(0.84f, 0.34f), new Vector2(0.72f, 0.44f) });
    }

    static void DrawLock(Color[] px)
    {
        DrawRoundedLine(px, 0.50f, 0.26f, 0.50f, 0.26f, 0f);
        DrawArc(px, 0.50f, 0.42f, 0.15f, 180f, 360f, 0.07f);
        DrawSquare(px, 0.28f, 0.42f, 0.72f, 0.78f, 0.07f);
        DrawCircle(px, 0.50f, 0.60f, 0.06f);
    }

    static void DrawPuzzle(Color[] px)
    {
        DrawSquare(px, 0.26f, 0.26f, 0.74f, 0.74f, 0.07f);
        DrawCircle(px, 0.26f, 0.38f, 0.10f);
        DrawCircle(px, 0.74f, 0.62f, 0.10f);
        DrawCircle(px, 0.50f, 0.74f, 0.10f);
    }

    static void DrawStar(Color[] px)
    {
        Vector2[] pts = new Vector2[10];
        for (int i = 0; i < 10; i++)
        {
            float r = i % 2 == 0 ? 0.34f : 0.14f;
            float ang = (i * 36f - 90f) * Mathf.Deg2Rad;
            pts[i] = new Vector2(0.50f + r * Mathf.Cos(ang), 0.50f + r * Mathf.Sin(ang));
        }
        DrawPoly(px, pts);
    }

    static void DrawWarning(Color[] px)
    {
        DrawPoly(px, new Vector2[] { new Vector2(0.50f, 0.16f), new Vector2(0.86f, 0.80f), new Vector2(0.14f, 0.80f) });
        DrawCircle(px, 0.50f, 0.60f, 0.06f);
        DrawRoundedLine(px, 0.50f, 0.34f, 0.50f, 0.52f, 0.06f);
    }

    static void DrawShield(Color[] px)
    {
        DrawPoly(px, new Vector2[]
        {
            new Vector2(0.50f, 0.16f), new Vector2(0.80f, 0.26f),
            new Vector2(0.80f, 0.52f), new Vector2(0.50f, 0.82f),
            new Vector2(0.20f, 0.52f), new Vector2(0.20f, 0.26f),
        });
        DrawRoundedLine(px, 0.50f, 0.34f, 0.50f, 0.34f, 0f);
        DrawRoundedLine(px, 0.38f, 0.46f, 0.47f, 0.55f, 0.06f);
        DrawRoundedLine(px, 0.47f, 0.55f, 0.63f, 0.40f, 0.06f);
    }

    static void DrawQrScan(Color[] px)
    {
        DrawRoundedLine(px, 0.24f, 0.28f, 0.24f, 0.40f, 0.07f);
        DrawRoundedLine(px, 0.24f, 0.72f, 0.24f, 0.60f, 0.07f);
        DrawRoundedLine(px, 0.76f, 0.28f, 0.76f, 0.40f, 0.07f);
        DrawRoundedLine(px, 0.76f, 0.72f, 0.76f, 0.60f, 0.07f);
        DrawRoundedLine(px, 0.28f, 0.24f, 0.40f, 0.24f, 0.07f);
        DrawRoundedLine(px, 0.72f, 0.24f, 0.60f, 0.24f, 0.07f);
        DrawRoundedLine(px, 0.28f, 0.76f, 0.40f, 0.76f, 0.07f);
        DrawRoundedLine(px, 0.72f, 0.76f, 0.60f, 0.76f, 0.07f);
        // QR blocks
        DrawSquare(px, 0.30f, 0.30f, 0.36f, 0.36f, 0.06f);
        DrawSquare(px, 0.34f, 0.42f, 0.42f, 0.50f, 0.06f);
        DrawSquare(px, 0.30f, 0.58f, 0.38f, 0.66f, 0.06f);
        DrawSquare(px, 0.50f, 0.30f, 0.58f, 0.38f, 0.06f);
        DrawSquare(px, 0.62f, 0.44f, 0.70f, 0.52f, 0.06f);
        DrawSquare(px, 0.50f, 0.56f, 0.58f, 0.64f, 0.06f);
        DrawSquare(px, 0.64f, 0.62f, 0.70f, 0.68f, 0.06f);
    }

    static void DrawEye(Color[] px)
    {
        DrawRing(px, 0.50f, 0.50f, 0.26f, 0.07f, false);
        DrawRing(px, 0.50f, 0.50f, 0.10f, 0.06f, false);
    }

    static void DrawTrash(Color[] px)
    {
        DrawRoundedLine(px, 0.32f, 0.24f, 0.68f, 0.24f, 0.06f);
        DrawRoundedLine(px, 0.28f, 0.24f, 0.34f, 0.76f, 0.06f);
        DrawRoundedLine(px, 0.72f, 0.24f, 0.66f, 0.76f, 0.06f);
        DrawRoundedLine(px, 0.44f, 0.34f, 0.44f, 0.64f, 0.05f);
        DrawRoundedLine(px, 0.56f, 0.34f, 0.56f, 0.64f, 0.05f);
    }

    static void DrawRing(Color[] px, float cx, float cy, float rOuter, float thickness, bool unused)
    {
        DrawRing(px, cx, cy, rOuter, thickness);
    }
}
#endif