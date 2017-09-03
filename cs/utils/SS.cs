using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SS : MonoBehaviour {
  // meaning: Standard Stuff.
  // 
  // Shorthand because I'm not going to write a [n unsolicited opinion on Israel???] novel every
  // time I want to get the smaller of two ints.
  
  public static int Max(int a, int b){
    return a>b?a:b;
  }
  public static int Min(int a, int b){
    return a<b?a:b;
  }
  
  public static int DistanceSquared(int x_a, int y_a, int x_b, int y_b){
    return ( (x_a-x_b)*(x_a-x_b) ) + ( (y_a - y_b) * (y_a - y_b) );
  }
  public static float DistanceSquared(float x_a, float y_a, float x_b, float y_b){
    return ( (x_a-x_b)*(x_a-x_b) ) + ( (y_a - y_b) * (y_a - y_b) );
  }
  
  public static int Abs(int a){
    return a<0?-a:a;
  }
  public static float Abs(float a){
    return a<0?-a:a;
    
  }
  
  public static float Difference(float a, float b){
    return a>b ? a - b : b - a;
  }
  public static int Difference(int a, int b){
    return a>b ? a - b : b - a;
  }
}
