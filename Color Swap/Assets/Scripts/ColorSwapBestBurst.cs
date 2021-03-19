using System;
using System.Diagnostics;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class ColorSwapBestBurst : MonoBehaviour, IColorSwapRandomizer
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
    var originalCubeTexture = _cubeMaterial.mainTexture as Texture2D;

    // Duplicate the texture and assign it to our object so we don't change the original
    _cubeTexture = new Texture2D(originalCubeTexture.width, originalCubeTexture.height,
      originalCubeTexture.format, originalCubeTexture.mipmapCount > 1);
    Graphics.CopyTexture(originalCubeTexture, _cubeTexture);
    _cubeMaterial.mainTexture = _cubeTexture;
  }

  public void RandomizeColor()
  {
    newColor = new Color32((byte) Random.Range(0, 256), (byte) Random.Range(0, 256), (byte) Random.Range(0, 256), 255);
  }

  void Update()
  {
    if (!SwapColor.AreColorsEqual(oldColor, newColor))
    {
      GC.Collect();
      var stopwatch = Stopwatch.StartNew();
      UpdateTextureColor();
      stopwatch.Stop();
      benchmarkSpeed.text = stopwatch.ElapsedMilliseconds + "ms\nGetRawTextureData+Jobs+Burst (Best)";
    }
  }

  void UpdateTextureColor()
  {
    var cubeArray = _cubeTexture.GetRawTextureData<Color32>();
    var colorToUpdate = oldColor;
    
    // We update this before starting the slow loop so that on the next frame we don't start another one
    oldColor = newColor;
    
    var job = new SwapColor()
    {
      cubeArray = cubeArray,
      colorToUpdate = colorToUpdate,
      newColor = newColor
    };

    // Split the cubeArray into chunks of 128 pixels and distribute it to all the threads for processing
    JobHandle handle = job.Schedule(cubeArray.Length, 128);
    JobHandle.ScheduleBatchedJobs(); // Start running (all) our previously scheduled jobs
    handle.Complete(); // Wait on the main thread fot this job to complete
    
    // Send the newly modified texture to the GPU
    _cubeTexture.Apply();
  }

  [BurstCompile] // <-- For this you need to add the Burst package using the Unity package manager
  public struct SwapColor : IJobParallelFor
  {
    public NativeArray<Color32> cubeArray;
    [ReadOnly] public Color32 colorToUpdate;
    [ReadOnly] public Color32 newColor;

    // This gets called by the job system. index ranges between 0 and the 'arrayLength' specified in the first parameter of Schedule()
    public void Execute(int pixelIndex)
    {
      if (AreColorsEqual(colorToUpdate, cubeArray[pixelIndex]))
      {
        cubeArray[pixelIndex] = newColor;
      }
    }

    public static bool AreColorsEqual(Color32 a, Color32 z)
    {
      return a.r == z.r
             && a.g == z.g
             && a.b == z.b;
    }
  }

}