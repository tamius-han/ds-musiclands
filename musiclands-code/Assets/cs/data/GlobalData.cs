using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlobalData : MonoBehaviour {
  
  public static List<Peak> peaks;
  public static string dataPath;
  public static string cachedir;
  
  
  // downloads
  public static int maxConcurrentDownloads = 3;
  public static int maxConcurrentBackgroundDownloads = 3;
  
  
  public static TerrainChunk[,] chunks;
  
  public static int songFetching_total;
  public static int songFetching_completed;
}
