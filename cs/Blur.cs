using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Blur : MonoBehaviour {

  private static float avgd = 0;
  private static float blurPixelCount = 0;
  
  
  // based off https://forum.unity3d.com/threads/contribution-texture2d-blur-in-c.185694/ | elecman
  
  public static float[,] FastBlur(float[,] data, int radius, int iterations){
    
    float[,] blurred = data;
    
    for (var i = 0; i < iterations; i++) {
      blurred = BlurData(blurred, radius, true);
      blurred = BlurData(blurred, radius, false);
    }
    
    return blurred;
  }
  
  static float[,] BlurData(float[,] data, int blurSize, bool horizontal){
    
    float[,] blurred = new float[data.GetLength(0),data.GetLength(1)];
    int _W = data.GetLength(0);
    int _H = data.GetLength(1);
    int xx, yy, x, y;
    
    if (horizontal) {
      
      for (yy = 0; yy < _H; yy++) {
        
        for (xx = 0; xx < _W; xx++) {
          
          ResetPixel();
          
          //Right side of pixel
          for ( x = xx; (x < xx + blurSize && x < _W); x++) {
            
            AddPixel(data[x, yy]);
          }
          
          //Left side of pixel
          for (x = xx; (x > xx - blurSize && x > 0); x--) {
            
            AddPixel(data[x, yy]);
          }
          
          CalcPixel();
          
          for (x = xx; x < xx + blurSize && x < _W; x++) {
            
            blurred[x, yy] = avgd;
          }
        }
      }
    }
    
    else {
      
      for (xx = 0; xx < _W; xx++) {
        
        for (yy = 0; yy < _H; yy++) {
          
          ResetPixel();
          
          //Over pixel
          for (y = yy; (y < yy + blurSize && y < _H); y++) {
            
            AddPixel(data[xx, y]);
          }
          
          //Under pixel
          for (y = yy; (y > yy - blurSize && y > 0); y--) {
            
            AddPixel(data[xx, y]);
          }
          
          CalcPixel();
          
          for (y = yy; y < yy + blurSize && y < _H; y++) {
            
            blurred[xx, y] = avgd;
          }
        }
      }
    }
    
    return blurred;
  }
  
  private static void AddPixel(float v) {
    avgd += v;
    blurPixelCount++;
  }
  
  private static void ResetPixel() {
    avgd = 0.0f;
    blurPixelCount = 0;
  }
  
  private static void CalcPixel() {    
    avgd = avgd / blurPixelCount;
  }
}
