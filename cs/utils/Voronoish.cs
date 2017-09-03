using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class Voronoish : MonoBehaviour {
  
  public static bool FINISHED = false;

  
  static int MAX_THREADS = 8;
  
  public static IEnumerator GenerateVoronoi(List<Peak> peaks, Terrain ter, int width, int chunkWidth){
    // width - terrain width
    
    int threads = MAX_THREADS; // todo: if we actually detected how many threads we can run
    object _lock_threads = new object();
    
    Texture2D terrainTexture = new Texture2D(width, width);
    
    // this makes us check distances in the middle of each chunk
    for(int i = 0; i < width; i += chunkWidth){
      for(int j = 0; j < width; j += chunkWidth){
        GenerateVoronoiChunk(i,j,width, chunkWidth, peaks, ter, terrainTexture);
      }
    }
    // wait for all threads to complete, the ghetto way
    
    while(threads < MAX_THREADS){
      System.Threading.Thread.Sleep(2000);
      print("still waiting for all threads to complete. Threads finished: " + threads);
      yield return null;
    }
    
    // let's apply texture to the terrain, actually
    
    SplatPrototype[] splattex = new SplatPrototype[1];
    splattex[0] = new SplatPrototype();
    splattex[0].texture = terrainTexture;
    splattex[0].tileSize = new Vector2(TerrainInit.TERRAIN_SIZE_ACTUAL,TerrainInit.TERRAIN_SIZE_ACTUAL);
    
    // add to terrain
    ter.terrainData.splatPrototypes = splattex;
    
    
    FINISHED = true;
//     return terrainTexture;
  }
  
  public static void GenerateVoronoiDebug(int posx, int posy, List<Peak> peaks){
    PriorityQueueF<Peak> nearestPeaks = new PriorityQueueF<Peak>("min");
    float peakDistance;    
    
    foreach(Peak p in peaks){
      peakDistance = (float)(SS.DistanceSquared(p.x, p.y, posx, posy));
      nearestPeaks.Enqueue(p, peakDistance);
    }
    
    List<Peak> nearestThree = new List<Peak>();
    nearestThree.Add(nearestPeaks.Dequeue());
    nearestThree.Add(nearestPeaks.Dequeue());
    nearestThree.Add(nearestPeaks.Dequeue());

    //distance from current point to peaks
    int distanceTo0, distanceTo1, distanceTo2, diff01, diff02, diff12, diffAll;
    
    
    distanceTo0 = SS.DistanceSquared(posx,posy,nearestThree[0].x, nearestThree[0].y);
    distanceTo1 = SS.DistanceSquared(posx,posy,nearestThree[1].x, nearestThree[1].y);
    distanceTo2 = SS.DistanceSquared(posx,posy,nearestThree[2].x, nearestThree[2].y);
    
    int mixingDistance = (int) (distanceTo0 * TerrainSettings.VONROISH_MIXING_DISTANCE);
    
    diff01 = distanceTo1 - distanceTo0; //if negative, 1 is closer than 0
    diff02 = distanceTo2 - distanceTo0;
    diff12 = distanceTo2 - distanceTo1;
    
    if( SS.Abs(diff01) < mixingDistance ){
      // we're mixing at least peaks 0 and 1
      if( SS.Abs(diff02) < mixingDistance){
        print("[Voronoish::test] — we mix all three peaks");
      }
      else{
        print("[Voronoish::test] — we mix only 0 and 1");
      }
    }
    else if(SS.Abs(diff02) < mixingDistance){
      print("[Voronoish::test] — we mix only 0 and 2");
    }
    else
      print("[Vonoroish::test] — we don't mix");
    
    print("We're at position " + posx + "," + posy + ". The closest three peaks are this far away: " + SS.DistanceSquared(nearestThree[0].x, nearestThree[0].y, posx, posy) + ";   " + SS.DistanceSquared(nearestThree[1].x, nearestThree[1].y, posx, posy) + ";    " + SS.DistanceSquared(nearestThree[2].x, nearestThree[2].y, posx, posy) );
    
    print("IDs of the closest three peaks are: " + nearestThree[0].id + ", " + nearestThree[1].id + ", " + nearestThree[2].id );
    
  }
  
  static void GenerateVoronoiChunk(int offset_x, int offset_y, int width, int chunkWidth, List<Peak> peaks, Terrain ter, Texture2D tex){
    
    if(peaks.Count < 3)
      return;
    
    int offset_middle = chunkWidth >> 1;
    
    int mid_x = offset_middle + offset_x;
    int mid_y = offset_middle + offset_y;
    
    PriorityQueueF<Peak> nearestPeaks = new PriorityQueueF<Peak>("min");
    float peakDistance;    
    foreach(Peak p in peaks){
      peakDistance = (float)(SS.DistanceSquared(p.x, p.y, mid_x, mid_y));
      nearestPeaks.Enqueue(p, peakDistance);
    }
    
    List<Peak> nearestThree = new List<Peak>();
    nearestThree.Add(nearestPeaks.Dequeue());
    nearestThree.Add(nearestPeaks.Dequeue());
    nearestThree.Add(nearestPeaks.Dequeue());
    
    // now that we have the three closest peaks, we can start doing the color magic
    
    //distance from current point to peaks
    int distanceTo0, distanceTo1, distanceTo2;
    
    // diff[a][b] — how much further is [b] compared to [a]
    int diff01, diff02, diff12, diffAll;
    
    //value from 0-1, part of peak's color in the final mix
    float partial0 = 1f;
    float partial1 = -1f;
    float partial2 = -1f;
    
    int mixingDistance;
    float normalized0, normalized1, normalized2;
    
    float[] rgbBase = new float[] {0.0f, 0.0f, 0.0f};
    float[] rgbSnow = new float[] {0.0f, 0.0f, 0.0f};
    
    float r, g, b; // tmp floats
    
    bool snowy;
    
    for(int i = offset_x; i < offset_x + chunkWidth; i++){
      for(int j = offset_y; j < offset_y + chunkWidth; j++){
        
        // first, let's calculate if we'll cover this pixel with snow:
        snowy = IsSnowy(i, j, width, ter.SampleHeight(new Vector3(i, 0, j)));
        
        distanceTo0 = SS.DistanceSquared(i,j,nearestThree[0].x, nearestThree[0].y);
        distanceTo1 = SS.DistanceSquared(i,j,nearestThree[1].x, nearestThree[1].y);
        distanceTo2 = SS.DistanceSquared(i,j,nearestThree[2].x, nearestThree[2].y);
        
        diff01 = distanceTo1 - distanceTo0; //if negative, 1 is closer than 0
        diff02 = distanceTo2 - distanceTo0;
        diff12 = distanceTo2 - distanceTo1;
        
        // we only do partial mixing if difference between points is within mixingDistance% of total distance to the nearest 
        // point for the chunk (distanceTo0)
        
        mixingDistance = (int) (distanceTo0 * TerrainSettings.VONROISH_MIXING_DISTANCE);
        
        if( SS.Abs(diff01) < mixingDistance ){
          // we're mixing at least peaks 0 and 1
          if( SS.Abs(diff02) < mixingDistance){
            //we're mixing all three
            
            // let's fix the distance first, normalize it to range 0~mixingDistance
            diff01 += mixingDistance;
            diff02 += mixingDistance;
            diff12 += mixingDistance;
            
            // dividing by this should give us proper ratios:
            diffAll = diff01 + diff02 + diff12;
            
            // now let's get partial values
            partial0 = ((float)diff01) / ((float)diffAll);
            partial1 = ((float)diff02) / ((float)diffAll);
            partial2 = ((float)diff12) / ((float)diffAll);
            
            // time to blend colors
            // special thanks to Lblaze from Spartan's How To Train Your Dragon discord server for reminding me
            // about colors actually being logarithmic, meaning ² and sqrt need to be used when blending
            
            if(snowy){
              r = Mathf.Sqrt( ((nearestThree[0].snowColor.r * nearestThree[0].snowColor.r) * partial0) +
                              ((nearestThree[1].snowColor.r * nearestThree[1].snowColor.r) * partial1) +
                              ((nearestThree[2].snowColor.r * nearestThree[2].snowColor.r) * partial2)
                            );
              g = Mathf.Sqrt( ((nearestThree[0].snowColor.g * nearestThree[0].snowColor.g) * partial0) +
                              ((nearestThree[1].snowColor.g * nearestThree[1].snowColor.g) * partial1) +
                              ((nearestThree[2].snowColor.g * nearestThree[2].snowColor.g) * partial2)
                            );
              b = Mathf.Sqrt( ((nearestThree[0].snowColor.b * nearestThree[0].snowColor.b) * partial0) +
                              ((nearestThree[1].snowColor.b * nearestThree[1].snowColor.b) * partial1) +
                              ((nearestThree[2].snowColor.b * nearestThree[2].snowColor.b) * partial2)
                            );
              }
            else{
              r = Mathf.Sqrt( ((nearestThree[0].baseColor.r * nearestThree[0].baseColor.r) * partial0) +
                              ((nearestThree[1].baseColor.r * nearestThree[1].baseColor.r) * partial1) +
                              ((nearestThree[2].baseColor.r * nearestThree[2].baseColor.r) * partial2)
                            );
              g = Mathf.Sqrt( ((nearestThree[0].baseColor.g * nearestThree[0].baseColor.g) * partial0) +
                              ((nearestThree[1].baseColor.g * nearestThree[1].baseColor.g) * partial1) +
                              ((nearestThree[2].baseColor.g * nearestThree[2].baseColor.g) * partial2)
                            );
              b = Mathf.Sqrt( ((nearestThree[0].baseColor.b * nearestThree[0].baseColor.b) * partial0) +
                              ((nearestThree[1].baseColor.b * nearestThree[1].baseColor.b) * partial1) +
                              ((nearestThree[2].baseColor.b * nearestThree[2].baseColor.b) * partial2)
                            );
            }
            
          }
          else{
            // we're mixing only the first and second
            
            // let's fix the distance first, normalize it to range 0~mixingDistance
            diff01 += mixingDistance;
            normalized1 = diff01 / mixingDistance;
            
            partial0 = normalized1;
            partial1 = 1f - normalized1;
            
            if(snowy){
              r = Mathf.Sqrt( ((nearestThree[0].snowColor.r * nearestThree[0].snowColor.r) * partial0) +
                              ((nearestThree[1].snowColor.r * nearestThree[1].snowColor.r) * partial1)
                            );
              g = Mathf.Sqrt( ((nearestThree[0].snowColor.g * nearestThree[0].snowColor.g) * partial0) +
                              ((nearestThree[1].snowColor.g * nearestThree[1].snowColor.g) * partial1)
                            );
              b = Mathf.Sqrt( ((nearestThree[0].snowColor.b * nearestThree[0].snowColor.b) * partial0) +
                              ((nearestThree[1].snowColor.b * nearestThree[1].snowColor.b) * partial1)
                            );
            }
            else{
              r = Mathf.Sqrt( ((nearestThree[0].baseColor.r * nearestThree[0].baseColor.r) * partial0) +
                              ((nearestThree[1].baseColor.r * nearestThree[0].baseColor.r) * partial1)
                            );
              g = Mathf.Sqrt( ((nearestThree[0].baseColor.g * nearestThree[0].baseColor.g) * partial0) +
                              ((nearestThree[1].baseColor.g * nearestThree[1].baseColor.g) * partial1)
                            );
              b = Mathf.Sqrt( ((nearestThree[0].baseColor.b * nearestThree[0].baseColor.b) * partial0) +
                              ((nearestThree[1].baseColor.b * nearestThree[1].baseColor.b) * partial1)
                            );
            }
          }
        }
        else if( SS.Abs(diff02) < mixingDistance ){
          // mixing first and third
          diff02 += mixingDistance;
          normalized1 = diff02 / mixingDistance;
          
          partial0 = normalized1;
          partial2 = 1f - normalized1;
          
          if(snowy){
            r = Mathf.Sqrt( ((nearestThree[0].snowColor.r * nearestThree[0].snowColor.r) * partial0) +
                            ((nearestThree[2].snowColor.r * nearestThree[2].snowColor.r) * partial1)
                          );
            g = Mathf.Sqrt( ((nearestThree[0].snowColor.g * nearestThree[0].snowColor.g) * partial0) +
                            ((nearestThree[2].snowColor.g * nearestThree[2].snowColor.g) * partial1)
                          );
            b = Mathf.Sqrt( ((nearestThree[0].snowColor.b * nearestThree[0].snowColor.b) * partial0) +
                            ((nearestThree[2].snowColor.b * nearestThree[2].snowColor.b) * partial1)
                          );
          }
          else{
            r = Mathf.Sqrt( ((nearestThree[0].baseColor.r * nearestThree[0].baseColor.r) * partial0) +
                            ((nearestThree[2].baseColor.r * nearestThree[0].baseColor.r) * partial1)
                          );
            g = Mathf.Sqrt( ((nearestThree[0].baseColor.g * nearestThree[0].baseColor.g) * partial0) +
                            ((nearestThree[2].baseColor.g * nearestThree[2].baseColor.g) * partial1)
                          );
            b = Mathf.Sqrt( ((nearestThree[0].baseColor.b * nearestThree[0].baseColor.b) * partial0) +
                            ((nearestThree[2].baseColor.b * nearestThree[2].baseColor.b) * partial1)
                          );
          }
        }
        else{
          // there's only one
          if( distanceTo0 <= distanceTo1 ){
            if(distanceTo0 <= distanceTo2){
              if(snowy){
                r = nearestThree[0].snowColor.r;
                g = nearestThree[0].snowColor.g;
                b = nearestThree[0].snowColor.b;
              }
              else{
                r = nearestThree[0].baseColor.r;
                g = nearestThree[0].baseColor.g;
                b = nearestThree[0].baseColor.b;
              }
            }
            else{
              if(snowy){
                r = nearestThree[2].snowColor.r;
                g = nearestThree[2].snowColor.g;
                b = nearestThree[2].snowColor.b;
              }
              else{
                r = nearestThree[2].baseColor.r;
                g = nearestThree[2].baseColor.g;
                b = nearestThree[2].baseColor.b;
              }
            }
          }
          else{
            if(distanceTo1 <= distanceTo2){
              if(snowy){
                r = nearestThree[1].snowColor.r;
                g = nearestThree[1].snowColor.g;
                b = nearestThree[1].snowColor.b;
              }
              else{
                r = nearestThree[1].baseColor.r;
                g = nearestThree[1].baseColor.g;
                b = nearestThree[1].baseColor.b;
              }
            }
            else{
              if(snowy){
                r = nearestThree[2].snowColor.r;
                g = nearestThree[2].snowColor.g;
                b = nearestThree[2].snowColor.b;
              }
              else{
                r = nearestThree[2].baseColor.r;
                g = nearestThree[2].baseColor.g;
                b = nearestThree[2].baseColor.b;
              }
            }
          }
        }
        
        // with our r,g,b values defined, we can now finally place a texture pixel
        
        tex.SetPixel(i,j,new Color(r,g,b));
      }
    }
  }
  
  static bool IsSnowy(int i, int j, int width, float height){
    float local_snowline_bottom_w;
    float local_snowline_bottom_e;
    float local_snowline_bottom;
    
    float local_snowline_upper_w;
    float local_snowline_upper_e;
    float local_snowline_upper;
    
    
    local_snowline_bottom_w = TerrainSettings.SNOWLINE_BOTTOM_SW + (j * (
      TerrainSettings.SNOWLINE_BOTTOM_NW - TerrainSettings.SNOWLINE_BOTTOM_SW
    ) / (width - i) );
    
    local_snowline_bottom_e = TerrainSettings.SNOWLINE_BOTTOM_SE + (j * (
      TerrainSettings.SNOWLINE_BOTTOM_NE - TerrainSettings.SNOWLINE_BOTTOM_SE
    ) / (width - i) );
    
    local_snowline_bottom = local_snowline_bottom_w + (i * (
      local_snowline_bottom_e - local_snowline_bottom_w
    ) / (width - j) );
    
    
    
    // interpolate top
    local_snowline_upper_w = TerrainSettings.SNOWLINE_UPPER_SW + (j * (
      TerrainSettings.SNOWLINE_UPPER_NW - TerrainSettings.SNOWLINE_UPPER_SW
    ) / (width - i) );
    
    local_snowline_upper_e = TerrainSettings.SNOWLINE_UPPER_SE + (j * (
      TerrainSettings.SNOWLINE_UPPER_NE - TerrainSettings.SNOWLINE_UPPER_SE
    ) / (width - i) );
    
    local_snowline_upper = local_snowline_upper_w + (i * (
      local_snowline_upper_e - local_snowline_upper_w
    ) / (width - j) );
    
    if ( height >= local_snowline_upper)
      return true;
    if ( height < local_snowline_bottom)
      return false;
    
    float local_snowline_upper_normalized = local_snowline_upper - local_snowline_bottom;
    float snowChance = (height - local_snowline_bottom) / local_snowline_upper_normalized;
    
    return Random.value < snowChance;
  }
  
}
