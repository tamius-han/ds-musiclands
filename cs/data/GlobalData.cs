using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlobalData : MonoBehaviour {
  
  public static List<Peak> peaks;
  public static string dataPath;
  public static string cachedir;
  
  // music point and chunk storage
  // ram is cheap, but higher levels (with more points) could take longer to re-process
  // so we're saving data of our parent levels for ez retreival.
  public static List<List<MusicPoint>> musicPointStack;
  public static List<TerrainChunk[,]> terrainChunkStack;
  public static List<TerrainTree> terrainTreeStack;
  
  // we also store locations where we performed descents
  public static List<float> descentHistory_x;
  public static List<float> descentHistory_y;
  
  public static int terrainMagnificationLevel = -1;
  
  public static bool bottomLevel = false;
  
  // if we're showing any menu or loading screen, this should be set to false.
  public static bool playerHasControl = false;
  
  // downloads
  public static int maxConcurrentDownloads = 3;
  public static int maxConcurrentBackgroundDownloads = 3;
  
  
  public static TerrainChunk[,] chunks;
  
  public static int songFetching_total;
  public static int songFetching_completed;
  
  // here's where we keep the boxes we put up as representation of song positions
  public static List<GameObject> songObjects;
  public static List<GameObject> peakMarkers;
  
}
