using System;
using System.Diagnostics;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class ColorSwapLowMemory : MonoBehaviour, IColorSwapRandomizer
{
  [SerializeField] private Color32 oldColor;
  [SerializeField] private Color32 newColor = Color.red;
  [SerializeField] private Text benchmarkSpeed;
  
  private Material _cubeMaterial;
  private Texture2D _cubeTexture;

  void Start()
  {
    // Get instance's material (not shared material, so changed only affect this object)
    _cubeMaterial = GetComponent<MeshRenderer>().material;
    // Original texture must be RGB or RGBA format, with no compression
    Texture2D originalCubeTexture = _cubeMaterial.mainTexture as Texture2D;

    // Duplicate the texture and assign it to our object so we don't change the original
    _cubeTexture = new Texture2D(originalCubeTexture.width, originalCubeTexture.height,
      originalCubeTexture.format, originalCubeTexture.mipmapCount > 1);
    Graphics.CopyTexture(originalCubeTexture, _cubeTexture);
    _cubeMaterial.mainTexture = _cubeTexture;
  }

  void Update()
  {
    if (!AreColorsEqual(oldColor, newColor))
    {
      GC.Collect();
      Stopwatch stopwatch = Stopwatch.StartNew();
      UpdateTextureColor();
      stopwatch.Stop();
      benchmarkSpeed.text = stopwatch.ElapsedMilliseconds + "ms\nGetRawTextureData (Low Memory)";
    }
  }
  
  public void RandomizeColor()
  {
    newColor = new Color32((byte) Random.Range(0, 256), (byte) Random.Range(0, 256), (byte) Random.Range(0, 256), 255);
  }
  
  void UpdateTextureColor()
  {
    // Doesn't create a new array, simply gives us a wrapper around the existing data in memory
    NativeArray<Color32> cubeArray = _cubeTexture.GetRawTextureData<Color32>();
    // We update this before starting the slow loop so that on the next frame we don't start another one
    Color32 colorToUpdate = oldColor;
    oldColor = newColor;

    // Loop across the texture array (backwards is faster for NativeArray), replacing any oldColor pixels with newColor
    for (int pixelIndex = 0; pixelIndex < cubeArray.Length; pixelIndex++)
    {
      if (AreColorsEqual(colorToUpdate, cubeArray[pixelIndex]))
      {
        cubeArray[pixelIndex] = newColor;
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