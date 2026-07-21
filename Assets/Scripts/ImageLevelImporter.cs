using System;
using UnityEngine;

public class ImageLevelImporter : MonoBehaviour
{
    [Header("Import Configuration")]
    [SerializeField] private Texture2D defaultLevelImage;
    [SerializeField] private string importMode = "outline"; // "outline", "dissolve", "spawners"

    private SandSimulationRenderer simulationRenderer;

    private void Start()
    {
        simulationRenderer = GetComponent<SandSimulationRenderer>();
    }

    // Call this to import the default configured image
    public void ImportDefaultLevel()
    {
        if (defaultLevelImage == null)
        {
            Debug.LogWarning("ImageLevelImporter: No default level image assigned!");
            return;
        }
        ImportImage(defaultLevelImage, importMode);
    }

    // Main entry point for dynamic level loading
    public void ImportImage(Texture2D image, string mode)
    {
        if (simulationRenderer == null)
        {
            simulationRenderer = GetComponent<SandSimulationRenderer>();
            if (simulationRenderer == null)
            {
                Debug.LogError("ImageLevelImporter: SandSimulationRenderer component is missing!");
                return;
            }
        }

        SandSimulation sim = simulationRenderer.GetSimulation();
        if (sim == null)
        {
            Debug.LogError("ImageLevelImporter: SandSimulation instance is null!");
            return;
        }

        Color32[] imageColors;
        try
        {
            imageColors = image.GetPixels32();
        }
        catch (UnityException e)
        {
            Debug.LogError($"ImageLevelImporter: FAILED to read texture. The texture '{image.name}' MUST be marked as 'Read/Write Enabled' in its Import Settings in the Inspector.\nDetails: {e.Message}");
            return;
        }

        int imgW = image.width;
        int imgH = image.height;
        int simW = sim.Width;
        int simH = sim.Height;

        sim.Clear();

        for (int y = 0; y < simH; y++)
        {
            // Flip y because simulation coordinates increase downwards (0 at top, simH-1 at bottom)
            // but Unity texture coordinates increase upwards (0 at bottom, imgH-1 at top)
            float normY = (float)y / simH;
            int texY = imgH - 1 - Mathf.FloorToInt(normY * imgH);
            texY = Mathf.Clamp(texY, 0, imgH - 1);

            for (int x = 0; x < simW; x++)
            {
                float normX = (float)x / simW;
                int texX = Mathf.FloorToInt(normX * imgW);
                texX = Mathf.Clamp(texX, 0, imgW - 1);

                Color32 pixel = imageColors[texY * imgW + texX];

                // Skip transparent pixels (alpha less than threshold)
                if (pixel.a < 50) continue;

                // Calculate brightness for outlines
                float brightness = (pixel.r * 0.299f) + (pixel.g * 0.587f) + (pixel.b * 0.114f);

                if (mode == "outline")
                {
                    // Dark pixels become solid wall obstacles, light pixels are ignored (remain Empty)
                    if (brightness < 130f)
                    {
                        sim.SetCell(x, y, SandElement.WALL);
                    }
                }
                else if (mode == "dissolve")
                {
                    // All pixels are converted into falling sand, maintaining their exact color
                    sim.SetCell(x, y, SandElement.SAND, pixel);
                }
                else if (mode == "spawners")
                {
                    // Spawn Map Mode mapping rules:
                    // - Grey/Black pixels -> wall
                    // - Bright Pink/Magenta -> Spawners
                    // - White pixels -> White Sand
                    // - Colored pixels -> Colored Sand
                    
                    bool isGrey = Math.Abs(pixel.r - pixel.g) < 20 && 
                                  Math.Abs(pixel.g - pixel.b) < 20 && 
                                  Math.Abs(pixel.r - pixel.b) < 20;

                    if (isGrey && brightness < 150f)
                    {
                        sim.SetCell(x, y, SandElement.WALL);
                    }
                    else if (pixel.r > 150 && pixel.g < 100 && pixel.b > 100)
                    {
                        // Spawner
                        sim.SetCell(x, y, SandElement.SPAWNER, pixel);
                    }
                    else if (pixel.r > 190 && pixel.g > 190 && pixel.b > 190)
                    {
                        // White Sand
                        sim.SetCell(x, y, SandElement.WHITE_SAND, new Color32(255, 255, 255, 255));
                    }
                    else
                    {
                        // Sand
                        sim.SetCell(x, y, SandElement.SAND, pixel);
                    }
                }
            }
        }

        Debug.Log($"ImageLevelImporter: Loaded image '{image.name}' in mode '{mode}'.");
    }

    // Wrapper endpoint methods for connecting UI triggers
    public void SetImportMode(string mode) => importMode = mode;
    public void SetDefaultImage(Texture2D image) => defaultLevelImage = image;
}
