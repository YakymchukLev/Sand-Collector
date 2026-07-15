using System;
using UnityEngine;

public static class SandElement
{
    public const byte EMPTY = 0;
    public const byte WALL = 1;
    public const byte SAND = 2;
    public const byte WATER = 3;
    public const byte SPAWNER = 4;
}

public class SandSimulation
{
    public int Width { get; private set; }
    public int Height { get; private set; }
    public int Size { get; private set; }

    public byte[] Types;
    public Color32[] Colors;
    public int[] LastUpdated;

    public int FrameIndex = 0;
    public string GravityDirection = "down"; // "down", "up", "left", "right"

    private int spawnerTimer = 0;
    private const int SPAWNER_INTERVAL = 2; // spawn every 2 frames

    private System.Random random;

    // Default elements color definitions
    public static readonly Color32 ColorEmpty = new Color32(0, 0, 0, 0);
    public static readonly Color32 ColorWall = new Color32(80, 80, 80, 255);
    public static readonly Color32 ColorSand = new Color32(240, 208, 96, 255);
    public static readonly Color32 ColorWater = new Color32(48, 144, 255, 255);
    public static readonly Color32 ColorSpawner = new Color32(192, 64, 192, 255);

    public SandSimulation(int width, int height)
    {
        Width = width;
        Height = height;
        Size = width * height;

        Types = new byte[Size];
        Colors = new Color32[Size];
        LastUpdated = new int[Size];

        random = new System.Random();
        Clear();
    }

    public void Clear()
    {
        Array.Clear(Types, 0, Size);
        Array.Clear(Colors, 0, Size);
        Array.Clear(LastUpdated, 0, Size);
        FrameIndex = 0;
    }

    public Color32 GetDefaultColor(byte type)
    {
        switch (type)
        {
            case SandElement.WALL: return ColorWall;
            case SandElement.SAND: return ColorSand;
            case SandElement.WATER: return ColorWater;
            case SandElement.SPAWNER: return ColorSpawner;
            default: return ColorEmpty;
        }
    }

    public byte GetCellType(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height) return SandElement.WALL; // Out of bounds is wall
        return Types[y * Width + x];
    }

    public Color32 GetCellColor(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height) return ColorWall;
        return Colors[y * Width + x];
    }

    public void SetCell(int x, int y, byte type, Color32? customColor = null)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height) return;
        int idx = y * Width + x;
        Types[idx] = type;
        Colors[idx] = customColor ?? GetDefaultColor(type);
        LastUpdated[idx] = FrameIndex;
    }

    public void DrawBrush(int cx, int cy, int radius, byte type, Color32? customColor = null)
    {
        int rSq = radius * radius;
        for (int dy = -radius; dy <= radius; dy++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                int x = cx + dx;
                int y = cy + dy;
                if (x >= 0 && x < Width && y >= 0 && y < Height)
                {
                    if (dx * dx + dy * dy <= rSq)
                    {
                        // Walls can't be painted over by sand/water, but Empty/Eraser clears walls, Walls paint over everything.
                        if (type == SandElement.EMPTY || type == SandElement.WALL || Types[y * Width + x] != SandElement.WALL)
                        {
                            SetCell(x, y, type, customColor);
                        }
                    }
                }
            }
        }
    }

    public void Update()
    {
        FrameIndex++;
        spawnerTimer++;

        if (spawnerTimer >= SPAWNER_INTERVAL)
        {
            spawnerTimer = 0;
            UpdateSpawners();
        }

        string dir = GravityDirection;

        if (dir == "down")
        {
            // Bottom-to-top sweep
            for (int y = Height - 1; y >= 0; y--)
            {
                bool leftToRight = random.NextDouble() < 0.5;
                if (leftToRight)
                {
                    for (int x = 0; x < Width; x++) UpdateCell(x, y);
                }
                else
                {
                    for (int x = Width - 1; x >= 0; x--) UpdateCell(x, y);
                }
            }
        }
        else if (dir == "up")
        {
            // Top-to-bottom sweep
            for (int y = 0; y < Height; y++)
            {
                bool leftToRight = random.NextDouble() < 0.5;
                if (leftToRight)
                {
                    for (int x = 0; x < Width; x++) UpdateCell(x, y);
                }
                else
                {
                    for (int x = Width - 1; x >= 0; x--) UpdateCell(x, y);
                }
            }
        }
        else if (dir == "left")
        {
            // Left-to-right sweep
            for (int x = 0; x < Width; x++)
            {
                bool topToBottom = random.NextDouble() < 0.5;
                if (topToBottom)
                {
                    for (int y = 0; y < Height; y++) UpdateCell(x, y);
                }
                else
                {
                    for (int y = Height - 1; y >= 0; y--) UpdateCell(x, y);
                }
            }
        }
        else if (dir == "right")
        {
            // Right-to-left sweep
            for (int x = Width - 1; x >= 0; x--)
            {
                bool topToBottom = random.NextDouble() < 0.5;
                if (topToBottom)
                {
                    for (int y = 0; y < Height; y++) UpdateCell(x, y);
                }
                else
                {
                    for (int y = Height - 1; y >= 0; y--) UpdateCell(x, y);
                }
            }
        }
    }

    private void UpdateSpawners()
    {
        for (int i = 0; i < Size; i++)
        {
            if (Types[i] == SandElement.SPAWNER)
            {
                int x = i % Width;
                int y = i / Width;
                Color32 color = Colors[i];

                int sx = x;
                int sy = y;
                if (GravityDirection == "down") sy += 1;
                else if (GravityDirection == "up") sy -= 1;
                else if (GravityDirection == "left") sx -= 1;
                else if (GravityDirection == "right") sx += 1;

                if (sx >= 0 && sx < Width && sy >= 0 && sy < Height)
                {
                    int targetIdx = sy * Width + sx;
                    if (Types[targetIdx] == SandElement.EMPTY)
                    {
                        // Spawn sand: use spawner color if custom, otherwise default sand color
                        bool isDefaultPink = color.r == ColorSpawner.r && color.g == ColorSpawner.g && color.b == ColorSpawner.b;
                        Color32 sandColor = isDefaultPink ? ColorSand : color;

                        Types[targetIdx] = SandElement.SAND;
                        Colors[targetIdx] = sandColor;
                        LastUpdated[targetIdx] = FrameIndex;
                    }
                }
            }
        }
    }

    private void UpdateCell(int x, int y)
    {
        int idx = y * Width + x;
        byte type = Types[idx];

        if (type == SandElement.EMPTY || type == SandElement.WALL || type == SandElement.SPAWNER) return;
        if (LastUpdated[idx] == FrameIndex) return;

        if (type == SandElement.SAND)
        {
            UpdateSand(x, y, idx);
        }
        else if (type == SandElement.WATER)
        {
            UpdateWater(x, y, idx);
        }
    }

    private void UpdateSand(int x, int y, int idx)
    {
        int dx = 0, dy = 0;
        if (GravityDirection == "down") dy = 1;
        else if (GravityDirection == "up") dy = -1;
        else if (GravityDirection == "left") dx = -1;
        else if (GravityDirection == "right") dx = 1;

        int nx = x + dx;
        int ny = y + dy;

        // Try direct movement
        if (TryMoveSand(idx, x, y, nx, ny)) return;

        // Diagonals
        int side1_x, side1_y, side2_x, side2_y;
        if (dy != 0) // vertical gravity
        {
            side1_x = x - 1; side1_y = y + dy;
            side2_x = x + 1; side2_y = y + dy;
        }
        else // horizontal gravity
        {
            side1_x = x + dx; side1_y = y - 1;
            side2_x = x + dx; side2_y = y + 1;
        }

        if (random.NextDouble() < 0.5)
        {
            if (TryMoveSand(idx, x, y, side1_x, side1_y)) return;
            if (TryMoveSand(idx, x, y, side2_x, side2_y)) return;
        }
        else
        {
            if (TryMoveSand(idx, x, y, side2_x, side2_y)) return;
            if (TryMoveSand(idx, x, y, side1_x, side1_y)) return;
        }
    }

    private bool TryMoveSand(int currIdx, int cx, int cy, int tx, int ty)
    {
        if (tx < 0 || tx >= Width || ty < 0 || ty >= Height) return false;

        int targetIdx = ty * Width + tx;
        byte targetType = Types[targetIdx];

        if (targetType == SandElement.EMPTY)
        {
            Types[targetIdx] = SandElement.SAND;
            Colors[targetIdx] = Colors[currIdx];
            LastUpdated[targetIdx] = FrameIndex;

            Types[currIdx] = SandElement.EMPTY;
            Colors[currIdx] = ColorEmpty;
            return true;
        }
        else if (targetType == SandElement.WATER)
        {
            // Sand sinks in water, swap
            Types[targetIdx] = SandElement.SAND;
            Color32 waterColor = Colors[targetIdx];
            Colors[targetIdx] = Colors[currIdx];
            LastUpdated[targetIdx] = FrameIndex;

            Types[currIdx] = SandElement.WATER;
            Colors[currIdx] = waterColor;
            LastUpdated[currIdx] = FrameIndex;
            return true;
        }

        return false;
    }

    private void UpdateWater(int x, int y, int idx)
    {
        int dx = 0, dy = 0;
        if (GravityDirection == "down") dy = 1;
        else if (GravityDirection == "up") dy = -1;
        else if (GravityDirection == "left") dx = -1;
        else if (GravityDirection == "right") dx = 1;

        int nx = x + dx;
        int ny = y + dy;

        // Try direct movement
        if (TryMoveWater(idx, x, y, nx, ny)) return;

        // Try diagonals
        int side1_x, side1_y, side2_x, side2_y;
        if (dy != 0) // vertical gravity
        {
            side1_x = x - 1; side1_y = y + dy;
            side2_x = x + 1; side2_y = y + dy;
        }
        else // horizontal gravity
        {
            side1_x = x + dx; side1_y = y - 1;
            side2_x = x + dx; side2_y = y + 1;
        }

        bool sideFirst = random.NextDouble() < 0.5;
        if (sideFirst)
        {
            if (TryMoveWater(idx, x, y, side1_x, side1_y)) return;
            if (TryMoveWater(idx, x, y, side2_x, side2_y)) return;
        }
        else
        {
            if (TryMoveWater(idx, x, y, side2_x, side2_y)) return;
            if (TryMoveWater(idx, x, y, side1_x, side1_y)) return;
        }

        // Spread perpendicular to gravity
        int spread1_x, spread1_y, spread2_x, spread2_y;
        if (dy != 0) // vertical gravity -> spread left/right
        {
            spread1_x = x - 1; spread1_y = y;
            spread2_x = x + 1; spread2_y = y;
        }
        else // horizontal gravity -> spread up/down
        {
            spread1_x = x; spread1_y = y - 1;
            spread2_x = x; spread2_y = y + 1;
        }

        sideFirst = random.NextDouble() < 0.5;
        if (sideFirst)
        {
            if (TryMoveWater(idx, x, y, spread1_x, spread1_y)) return;
            if (TryMoveWater(idx, x, y, spread2_x, spread2_y)) return;
        }
        else
        {
            if (TryMoveWater(idx, x, y, spread2_x, spread2_y)) return;
            if (TryMoveWater(idx, x, y, spread1_x, spread1_y)) return;
        }
    }

    private bool TryMoveWater(int currIdx, int cx, int cy, int tx, int ty)
    {
        if (tx < 0 || tx >= Width || ty < 0 || ty >= Height) return false;

        int targetIdx = ty * Width + tx;
        byte targetType = Types[targetIdx];

        if (targetType == SandElement.EMPTY)
        {
            Types[targetIdx] = SandElement.WATER;
            Colors[targetIdx] = Colors[currIdx];
            LastUpdated[targetIdx] = FrameIndex;

            Types[currIdx] = SandElement.EMPTY;
            Colors[currIdx] = ColorEmpty;
            return true;
        }

        return false;
    }
}
