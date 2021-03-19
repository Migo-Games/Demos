using System;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class ColorSwapFast : MonoBehaviour, IColorSwapRandomizer
{
  [SerializeField] private Color32 oldColor;
  [SerializeField] private Color32 newColor = Color.red;
  [SerializeField] private Text benchmarkSpeed;
  
  private Material _cubeMaterial;
  private Texture2D _cubeTexture;
  private Color32[] _cubeArray;

  void Start()
  {
    // Get instance's material (not shared material, so changed only affect this object)
    _cubeMaterial = GetComponent<MeshRenderer>().material;
    // Original texture must be RGB or RGBA format, with no compression
    var originalCubeTexture = _cubeMaterial.mainTexture as Texture2D;

    // Duplicate the texture and assign it to our object so we don't change the original
    _cubeTexture = new Texture2D(originalCubeTexture.width, originalCubeTexture.height,
      originalCubeTexture.format, originalCubeTexture.mipmapCount > 1);
    Graphics.CopyTexture(originalCubeTexture, _cubeTexture);
    _cubeMaterial.mainTexture = _cubeTexture;
    _cubeArray = _cubeTexture.GetPixels32();
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
      benchmarkSpeed.text = stopwatch.ElapsedMilliseconds + "ms\nGetPixels32 (Fast)";
    }
  }

  void UpdateTextureColor()
  {
    // We update this before starting the slow loop so that on the next frame we don't start another one
    Color32 colorToUpdate = oldColor;
    oldColor = newColor;

    // Loop across the texture array, replacing any oldColor(colorToUpdate) pixels with newColor
    for (int pixelIndex = 0; pixelIndex < _cubeArray.Length; pixelIndex++)
    {
      if (AreColorsEqual(colorToUpdate, _cubeArray[pixelIndex]))
      {
        _cubeArray[pixelIndex] = newColor;
      }
    }

    _cubeTexture.SetPixels32(_cubeArray);
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