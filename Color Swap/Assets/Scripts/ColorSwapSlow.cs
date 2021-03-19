using System;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class ColorSwapSlow : MonoBehaviour, IColorSwapRandomizer
{
  [SerializeField] private Color oldColor;
  [SerializeField] private Color newColor = Color.red;
  [SerializeField] private Text benchmarkSpeed;
  
  private Texture2D _cubeTexture;

  void Start()
  {
    // Get instance's material (not shared material, so changed only affect this object)
    var cubeMaterial = GetComponent<MeshRenderer>().material;
    // Original texture must be RGBA format, with no compression
    var originalCubeTexture = cubeMaterial.mainTexture as Texture2D;

    // Duplicate the texture and assign it to our object so we don't change the original
    _cubeTexture = new Texture2D(originalCubeTexture.width, originalCubeTexture.height,
      originalCubeTexture.format, originalCubeTexture.mipmapCount > 1);
    Graphics.CopyTexture(originalCubeTexture, _cubeTexture);
    cubeMaterial.mainTexture = _cubeTexture;
  }
  
  public void RandomizeColor()
  {
    newColor = new Color32((byte) Random.Range(0, 256), (byte) Random.Range(0, 256), (byte) Random.Range(0, 256), 255);
  }

  void Update()
  {
    if (!AreColorsEqual(oldColor, newColor))
    {
      GC.Collect();
      var stopwatch = Stopwatch.StartNew();
      UpdateTextureColor();
      stopwatch.Stop();
      benchmarkSpeed.text = stopwatch.ElapsedMilliseconds + "ms\nGetPixel (Slow)";
    }
  }

  void UpdateTextureColor()
  {
    // We update this before starting the slow loop so that on the next frame we don't start another one
    var colorToUpdate = oldColor;
    oldColor = newColor;

    // Loop across the texture, replacing any oldColor(colorToUpdate) pixels with newColor
    for (var x = 0; x < _cubeTexture.width; x++)
    {
      for (var y = 0; y < _cubeTexture.width; y++)
      {
        if (AreColorsEqual(colorToUpdate, _cubeTexture.GetPixel(x, y)))
        {
          _cubeTexture.SetPixel(x, y, newColor);
        }
      }
    }

    // Send the newly modified texture to the GPU
    _cubeTexture.Apply();
  }

  // Color32 is used here since Color can be implicitly converted and comparing floats often fails
  private static bool AreColorsEqual(Color32 a, Color32 z)
  {
    return a.r == z.r
           && a.g == z.g
           && a.b == z.b;
  }
}