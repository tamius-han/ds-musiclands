using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class misc : MonoBehaviour {
  /***********************************************************************************************************/
  //BEGIN interpolation
  //TODO — move to its own class
  static float[,] BicubicInterpolate(float[,] inmap, int width, int height){
    return BicubicInterpolate(inmap, width, height, 1.0f);
  }
  
  static float[,] BicubicInterpolate(float[,] inmap, int width, int height, float multiplier){
    float[,] interpolated = new float[width,height];
    
    int org_width = inmap.GetLength(0);
    int org_height = inmap.GetLength(1);
    
    float org_x, org_y;
    int org_x_index, org_y_index;
    float offset_x, offset_y; 
    
    float step_x, step_y;
    step_x = ((float)org_width) / ((float)width);
    step_y = ((float)org_height) / ((float)height);
    
    float x0, x1, x2, x3;
    int low_x, high_x, high_x_2;
    int low_y, high_y, high_y_2;
    
    
    
    // We need to zero-pad the inmap before doing anything, so interpolation works properly without index-out-of-bounds
    // exceptions we'd otherwise get
    float[,] tmpin = new float[org_width+3, org_height+3];
    
    // Step 1: copy all the values from the old array
    for(int i = 0; i < org_width; i++){
      for(int j = 0; j < org_height; j++){
        tmpin[i+1,j+1] = inmap[i,j];
      }
    }
    
    // Step 2: add zeroes to the first, last and second-last column and row
    int lastCol = org_width + 2;
    int secondLastCol = org_width + 1;
    
    int lastRow = org_height + 2;
    int secondLastRow = org_height + 1;
    
    for(int i = 0; i < tmpin.GetLength(0); i++){
      tmpin[i,0] = tmpin[i,0];
      tmpin[i,lastRow] = tmpin[i,secondLastRow-1];
      tmpin[i,secondLastRow] = tmpin[i,lastRow];
    }
    for(int i = 0; i < tmpin.GetLength(1); i++){
      tmpin[0,i] = tmpin[1,i];
      tmpin[lastCol,i] = tmpin[secondLastCol-1,i];
      tmpin[secondLastCol,i] = tmpin[lastCol, i];
    }
    
    inmap = null;
    inmap = tmpin;
    
    // Step 3: actually do the interpolation
    
    for(int new_x = 0; new_x < width; new_x++){
      org_x = new_x * step_x;
      
      org_x_index = (int)Mathf.Floor(org_x) + 1;
      offset_x = org_x % 1;
      
      
      low_x    = org_x_index - 1;
      high_x   = org_x_index + 1; 
      high_x_2 = org_x_index + 2;
      
      for(int new_y = 0; new_y < height; new_y++){
        org_y = new_y * step_y;
        org_y_index = (int)Mathf.Floor(org_y) + 1;
        offset_y = org_y % 1;
        
        low_y    = org_y_index - 1;
        high_y   = org_y_index + 1;
        high_y_2 = org_y_index + 2;
        
        //         print("here are the limits: " + inmap.GetLength(0) + "," inmap.GetLength(1) );
        //         print("here are the values: " 
        try{
          x0 = BicubicInterpolateValues(inmap[low_x,       low_y],
                                        inmap[org_x_index, low_y], 
                                        inmap[high_x,      low_y], 
                                        inmap[high_x_2,    low_y],
                                        offset_x
          );
          x1 = BicubicInterpolateValues(inmap[low_x,       org_y_index],
                                        inmap[org_x_index, org_y_index],
                                        inmap[high_x,      org_y_index],
                                        inmap[high_x_2,    org_y_index],
                                        offset_x
          );
          x2 = BicubicInterpolateValues(inmap[low_x,       high_y],
                                        inmap[org_x_index, high_y],
                                        inmap[high_x,      high_y],
                                        inmap[high_x_2,    high_y],
                                        offset_x
          );
          x3 = BicubicInterpolateValues(inmap[low_x,       high_y_2],
                                        inmap[org_x_index, high_y_2],
                                        inmap[high_x,      high_x_2],
                                        inmap[high_x_2,    high_y_2],
                                        offset_x
          );
          interpolated[new_x,new_y] = BicubicInterpolateValues( x0, x1, x2, x3, offset_y ) * multiplier;
          //           interpolated[new_x,new_y] = NearestNeighbour(x1,x2,org_y % 1) * multiplier;
          //           interpolated[new_x,new_y] = Linear(x2,x3,org_y % 1) * multiplier;
          //           interpolated[new_x, new_y] = CosInterpolate(x2,x3,org_y % 1) * multiplier;
          //           interpolated[new_x, new_y] = CosInterpolate(CosInterpolate(inmap[org_x_index, org_y_index],
          //                                                                      inmap[org_x_index, high_y],
          //                                                                      org_y % 1
          //                                                                      ),
          //                                                       CosInterpolate(inmap[high_x, org_y_index],
          //                                                                      inmap[high_x, high_y],
          //                                                                      org_y % 1
          //                                                                     ),
          //                                                       org_x % 1
          //                                                      ) * multiplier;
        }catch (Exception e){
          print("Index out of range. Our range is this: [" + inmap.GetLength(0) + "," + inmap.GetLength(1) + "]" +
          "\nour values are:\n" + 
          "                                  low: [" + low_x + "," + low_y + "]\n" +
          "                            org_index: [" + org_x_index + "," + org_y_index + "]\n" +
          "                                 high: [" + high_x + "," + high_y + "]\n" + 
          "                               high_2: [" + high_x_2 + "," + high_y_2 + "]\n"
          );
        }
      }
    }
    
    return interpolated;
  }
  
  static float NearestNeighbour(float x0, float x1, float offset){
    return (offset < 0.5f) ? x0 : x1;
  }
  static float Linear(float x0, float x1, float offset){
    return (1.0f-offset)*x0 + offset*x1;
  }
  static float CosInterpolate(float x0, float x1, float offset){
    
    float tmp = (1-Mathf.Cos(offset * Mathf.PI))/2;
    return x0 * (1-tmp) + x1 * tmp;
  }
  
  static float BicubicInterpolateValues(float x0, float x1, float x2, float x3, float offset){
    // source: http://paulbourke.net/miscellaneous/interpolation/
    //         http://www.paulinternet.nl/?page=bicubic
    float a0, a1, a2, a3, offsetsq, interpolated;
    
    offsetsq = offset*offset;
    //     a0 = x3 - x2 - x0 + x1;
    //     a1 = x0 - x1 - a0;
    //     a2 = x2 - x0;
    //     a3 = x1;
    
    a0 = (-0.5f * x0) + (1.5f * x1) - (1.5f * x2) + (0.5f * x3);
    a1 = x0 - (2.5f * x1) + (2.0f * x2) - (0.5f * x3);
    a2 = (-0.5f * x0) + (0.5f * x2);
    a3 = x1;
    
    interpolated = (a0 * offset * offsetsq) + (a1 * offsetsq) + (a2 * offset) + a3;
    
    return interpolated;
    
    //     a0 = 3.0f * (x1 - x2) + x3 - x1;
    //     a1 = 2.0f * x0 - 5.0f*x1 + 4.0f*x2 - x3 + offset*a0;
    //     a2 = x2 - x0 + offset*a1;
    //     a3 = x1 + 0.5f * offset * a2;
    //     
    //     return a3;
    //     
    //     return x1 + 0.5f * offset*(x2 - x0 + offset*(2.0f*x0 - 5.0f*x1 + 4.0f*x2 - x3 + offset*(3.0f*(x1 - x2) + x3 - x0)));
  }
  //END interpolation
  
  
  /***********************************************************************************************************/
	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
