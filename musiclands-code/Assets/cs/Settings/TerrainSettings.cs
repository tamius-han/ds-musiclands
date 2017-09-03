using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainSettings {
  public static readonly int PEAK_SONG_RADIUS=3;
  public static readonly int PEAK_SONGCOUNT_MAX=100;
  public static readonly float LOWEST_ALLOWED_PEAK_HEIGHT=8.8f;
  
  
  // % of distance to the closest peak where colors between different
  // chunks get blended together 
  public static readonly float VONROISH_MIXING_DISTANCE = 0.0f;
  
  // Snow starts to appear above these heights:
  public static readonly float SNOWLINE_BOTTOM_SW = 35f;
  public static readonly float SNOWLINE_BOTTOM_SE = 30f;
  public static readonly float SNOWLINE_BOTTOM_NW = 40f;
  public static readonly float SNOWLINE_BOTTOM_NE = 40f;
  
  // only snow above this line
  public static readonly float SNOWLINE_UPPER_SW = 55f;
  public static readonly float SNOWLINE_UPPER_SE = 65f;
  public static readonly float SNOWLINE_UPPER_NW = 65f;
  public static readonly float SNOWLINE_UPPER_NE = 65f;
  
  // number of spaces away from chunk border we start attempting to prefetch
  public static readonly int   GPM_PREFETCH_TRESHOLD = 5;
  
  // number of samples away from song end we try to prefetch the next song in the queue (1s = 44000)
  public static readonly int   GPM_PREFETCH_NEXT_TRESHOLD_SAMPLES = 250000;  // just over 5 seconds
  
  // song must be long at least this many samples or we ignore the above treshold
  public static readonly int   GPM_PREFETCH_NEXT_MINIMUM_SAMPLES  = 1300000; // just unde 30 sec
}
