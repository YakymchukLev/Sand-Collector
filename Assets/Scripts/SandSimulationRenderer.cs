using System;
using UnityEngine;
using UnityEngine.UI;

public class SandSimulationRenderer : MonoBehaviour
{
    [Header("Simulation Configuration")]
    [SerializeField] private int width = 300;
    [SerializeField] private int height = 200;
    [SerializeField] private int simulationStepsPerFrame = 1;
    [SerializeField] private bool isPlaying = true;
    [SerializeField] private string gravityDirection = "down";

    [Header("Drawing Configuration")]
    [Range(1, 15)] [SerializeField] private int brushRadius = 4;
    [SerializeField] private byte selectedElementType = SandElement.SAND;
    [SerializeField] private Color32 customBrushColor = new Color32(240, 208, 96, 255);
    [SerializeField] private bool useCustomColor = false;

    [Header("Unity UI Component Link")]
    [SerializeField] private RawImage displayImage;

    // Core variables
    private SandSimulation simulation;
    private Texture2D texture;
    private Color32[] texturePixels;

    // Drawing trace variables
    private bool isMousePressed = false;
    private int? prevMouseX = null;
    private int? prevMouseY = null;

    public SandSimulation GetSimulation() => simulation;

    private void Start()
    {
        InitializeSimulation();
    }

    private void InitializeSimulation()
    {
        if (displayImage == null)
        {
            displayImage = GetComponent<RawImage>();
            if (displayImage == null)
            {
                Debug.LogError("SandSimulationRenderer: Display RawImage is not assigned!");
                return;
            }
        }

        simulation = new SandSimulation(width, height);
        simulation.GravityDirection = gravityDirection;

        // Create Unity Texture
        texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point; // Crisp pixels
        texture.wrapMode = TextureWrapMode.Clamp;
        
        displayImage.texture = texture;

        texturePixels = new Color32[width * height];
        
        // Push initial state
        RenderSimulationToTexture();
    }

    private void Update()
    {
        if (simulation == null) return;

        // Sync Inspector configurations to simulation state
        simulation.GravityDirection = gravityDirection;

        // 1. Run physics steps if playing
        if (isPlaying)
        {
            for (int s = 0; s < simulationStepsPerFrame; s++)
            {
                simulation.Update();
            }
        }

        // 2. Handle Drawing Input
        HandleInput();

        // 3. Render grid colors to Unity texture
        RenderSimulationToTexture();
    }

    private void HandleInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            isMousePressed = true;
            DrawAtMousePosition();
        }
        else if (Input.GetMouseButton(0) && isMousePressed)
        {
            DrawAtMousePosition();
        }
        else if (Input.GetMouseButtonUp(0))
        {
            isMousePressed = false;
            prevMouseX = null;
            prevMouseY = null;
        }
    }

    private void DrawAtMousePosition()
    {
        if (displayImage == null) return;

        RectTransform rectTransform = displayImage.rectTransform;
        Vector2 localPoint;

        // Convert mouse screen position to local space coordinate on UI image
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, Input.mousePosition, null, out localPoint))
        {
            Rect rect = rectTransform.rect;

            // Normalize coordinate (ranges from 0.0 to 1.0)
            float normX = (localPoint.x - rect.xMin) / rect.width;
            float normY = (localPoint.y - rect.yMin) / rect.height;

            // Map to grid coordinates
            int gridX = Mathf.FloorToInt(normX * width);
            // Flip y because simulation coordinates increase downwards (0 at top, height-1 at bottom)
            int gridY = Mathf.FloorToInt((1f - normY) * height);

            if (gridX >= 0 && gridX < width && gridY >= 0 && gridY < height)
            {
                Color32? brushColor = useCustomColor ? (Color32?)customBrushColor : null;

                if (prevMouseX.HasValue && prevMouseY.HasValue)
                {
                    // Interpolate continuous line drawing
                    DrawLine(prevMouseX.Value, prevMouseY.Value, gridX, gridY, brushRadius, selectedElementType, brushColor);
                }
                else
                {
                    simulation.DrawBrush(gridX, gridY, brushRadius, selectedElementType, brushColor);
                }

                prevMouseX = gridX;
                prevMouseY = gridY;
            }
            else
            {
                // Reset drawing trace if dragged out of canvas boundaries
                prevMouseX = null;
                prevMouseY = null;
            }
        }
    }

    // Bresenham's line algorithm to prevent gaps when drawing rapidly
    private void DrawLine(int x0, int y0, int x1, int y1, int radius, byte type, Color32? color)
    {
        int dx = Math.Abs(x1 - x0);
        int dy = Math.Abs(y1 - y0);
        int sx = (x0 < x1) ? 1 : -1;
        int sy = (y0 < y1) ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            simulation.DrawBrush(x0, y0, radius, type, color);
            if (x0 == x1 && y0 == y1) break;
            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }
    }

    private void RenderSimulationToTexture()
    {
        if (texture == null || simulation == null) return;

        // Flip rows vertically while copying simulation grid colors to Texture2D buffer.
        // Simulation: row 0 is top. Unity texture: row 0 is bottom.
        for (int y = 0; y < height; y++)
        {
            int simY = y;
            int texY = height - 1 - y;
            Array.Copy(simulation.Colors, simY * width, texturePixels, texY * width, width);
        }

        // Apply updated color array to Texture2D
        texture.SetPixels32(texturePixels);
        texture.Apply();
    }

    // Public controller endpoints to connect to Unity Buttons/Dropdowns
    public void SetElementType(int typeIndex) => selectedElementType = (byte)typeIndex;
    public void SetBrushRadius(float radius) => brushRadius = Mathf.RoundToInt(radius);
    public void SetStepsPerFrame(float steps) => simulationStepsPerFrame = Mathf.RoundToInt(steps);
    public void SetPlayPause(bool playState) => isPlaying = playState;
    public void TogglePlayPause() => isPlaying = !isPlaying;
    public void SetGravity(string direction) => gravityDirection = direction;
    public void ClearSimulation() => simulation?.Clear();
}
