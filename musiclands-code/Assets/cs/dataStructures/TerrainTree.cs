using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainLodLevel {
  public int depth;
  public float multiplier;
  
  public TerrainLodLevel(int depth, float multiplier){
    // depth: 0 - leaf, max resolution 
    // 
    this.depth = depth;
    this.multiplier = multiplier;
  }
  
}

public class TerrainTree {
  // aka 'shitty quad tree'
  
  TerrainTree[,] subtrees;  // array of 2x2 subtrees
  float midpoint_x;
  float midpoint_y;          
  float width;
  int datapoints;                  // how many songs does this tree have
  float averageHeight;            // height of this segment
  int level;                     // how deep in the hierarchy this tree is. 0 — leaf
  public float average_x;       // cummulative offset from midpoint_x. Can be converted to average offset when tree is built
  public float average_y;      // cummulative offset from midpoint_y. Can be converted to average offset when tree is built
  /**
   *     How levels work
   *      
   *             xx            l(n - 1)
   *            /  \
   *           /    \
   *          x      x         l(n - 2)
   *         / \    / \
   *          ...   ...
   *       /   \        
   *      x     x   ...        l2
   *     /|     |\    
   *    x x     x x   ...      l1
   *   /| |\   /| |\  
   *  x x x x x x x x    ...   l0
   */
  
  List<MusicPoint> songs;  // if we're in a leaf, this list contains all the songs.
  
  public TerrainTree(int level){
    // bad code, bad code, what you gonna do?
    // what you gonna do when it comes for you
    // 
    // Note: 
    
    this.level = level;
    
    if(level == 0){
      // leaf level is a special case
      this.width = 1;
      this.midpoint_x = 0.5f;
      this.midpoint_y = 0.5f;
    }
    else{
      this.width = (float)(1 << level);  // width should be a power of two for ez
      this.midpoint_x = (float)(1 << (level - 1));
      this.midpoint_y = this.midpoint_x;
    }
    
    this.datapoints = 0;
    this.averageHeight = 0f;
    this.average_x = 0f;
    this.average_y = 0f;
    
    this.subtrees = null;
  }
  
  public TerrainTree(int level, float width, float midpoint_x, float midpoint_y){
    this.level = level;
    
    this.datapoints = 0;
    this.averageHeight = 0;
    
    this.width = width;
    
    this.midpoint_x = midpoint_x;
    this.midpoint_y = midpoint_y;
    
    if (this.level != 0)
      this.subtrees = null;
    else
      this.songs = new List<MusicPoint>();
  }
  
  public void InsertSong(MusicPoint mp){
    this.average_x += mp.x - midpoint_x;
    this.average_y += mp.y - midpoint_y;   
    
    if( this.level == 0 ){
      this.songs.Add(mp);
      this.datapoints++;
      return;
    }
    
    if( this.subtrees == null ){
      float childWidth = this.width * 0.5f;
      float childWidthHalf = this.width * 0.25f;
      int childLevel = this.level - 1;
      
      this.subtrees = new TerrainTree[2,2];
      
      this.subtrees[0,0] = new TerrainTree(childLevel, childWidth,
                                           this.midpoint_x - childWidthHalf,
                                           this.midpoint_y - childWidthHalf);
      this.subtrees[0,1] = new TerrainTree(childLevel, childWidth,
                                           this.midpoint_x - childWidthHalf,
                                           this.midpoint_y + childWidthHalf);
      this.subtrees[1,0] = new TerrainTree(childLevel, childWidth,
                                           this.midpoint_x + childWidthHalf,
                                           this.midpoint_y - childWidthHalf);
      this.subtrees[1,1] = new TerrainTree(childLevel, childWidth,
                                           this.midpoint_x + childWidthHalf,
                                           this.midpoint_y + childWidthHalf);
    }
    
    // if we're at this point, this means we get to add this song into one of the four subtrees
    this.datapoints++;
    
    int stx = mp.x < this.midpoint_x ? 0 : 1;
    int sty = mp.y < this.midpoint_y ? 0 : 1;
    
    subtrees[stx,sty].InsertSong(mp);
  }
  
  public void CalculateAverageHeight(){
    this.averageHeight = (float)datapoints / (width * width);
    
    if( subtrees == null )
      return;
    
    subtrees[0,0].CalculateAverageHeight();
    subtrees[0,1].CalculateAverageHeight();
    subtrees[1,0].CalculateAverageHeight();
    subtrees[1,1].CalculateAverageHeight();
  }
  
  public void CalculateAveragePosition(){
    this.average_x /= (float)datapoints;
    this.average_y /= (float)datapoints;
    
    this.average_x += midpoint_x;
    this.average_y += midpoint_y;
    
    if(subtrees == null)
      return;
    
    subtrees[0,0].CalculateAveragePosition();
    subtrees[0,1].CalculateAveragePosition();
    subtrees[1,0].CalculateAveragePosition();
    subtrees[1,1].CalculateAveragePosition();
  }
  
  public int CountSongsInArea(int targetLevel, float x, float y){
    if(this.level > targetLevel){
      
      int stx = (x < this.midpoint_x) ? 0 : 1;
      int sty = (y < this.midpoint_y) ? 0 : 1;
      
      if(subtrees != null && subtrees[stx, sty] != null){
        return subtrees[stx, sty].CountSongsInArea(targetLevel, x, y);
      }
      return 0;
    }
    return this.datapoints;
  }
  
  public TerrainTree GetSubtreeAt(int targetLevel, float x, float y){
    if(this.level > targetLevel){
      
      int stx = (x < this.midpoint_x) ? 0 : 1;
      int sty = (y < this.midpoint_y) ? 0 : 1;
      
      if(subtrees != null && subtrees[stx, sty] != null){
        return subtrees[stx, sty].GetSubtreeAt(targetLevel, x, y);
      }
      return null;
    }
    return this;
  }
  
  public List<MusicPoint> GetSongsInArea(int targetLevel, float x, float y){
    if(this.level > targetLevel){
      
      int stx = (x < this.midpoint_x) ? 0 : 1;
      int sty = (y < this.midpoint_y) ? 0 : 1;
      
      if(subtrees != null && subtrees[stx, sty] != null)
        return subtrees[stx, sty].GetSongsInArea(targetLevel, x, y);
      return new List<MusicPoint>();
    }
    
    return this.GetAllSongs();
  }
  
  public void GetSongsInArea(int targetLevel, List<MusicPoint>[,] mpOut, float x, float y){
    if(this.level > targetLevel){
      
      int stx = (x < this.midpoint_x) ? 0 : 1;
      int sty = (y < this.midpoint_y) ? 0 : 1;
      
      if(subtrees != null && subtrees[stx, sty] != null){
        subtrees[stx, sty].GetSongsInArea(targetLevel, mpOut, x, y);
        return;
      }
      mpOut = null;
      return;
    }
    
    this.GetAllSongs(mpOut, 0, 0, 1 << this.level);
    
   return;
  }
  
  public List<MusicPoint> GetAllSongs(){
    
    if(this.level == 0)
      return songs;
    
    List<MusicPoint> allSongs = new List<MusicPoint>();
    
    if(subtrees != null){
      allSongs.AddRange(subtrees[0,0].GetAllSongs());
      allSongs.AddRange(subtrees[0,1].GetAllSongs());
      allSongs.AddRange(subtrees[1,0].GetAllSongs());
      allSongs.AddRange(subtrees[1,1].GetAllSongs());
    }
    return allSongs;
  }
  
  public void GetAllSongs(List<MusicPoint>[,] mpOut, int x, int y, int width){
    
    if(this.level == 0){
      mpOut[x,y] = this.songs;
      return;
    }
    
    if(this.subtrees == null){
      for(int i = x; i < x+width; i++)
        for(int j = y; j <y+width; j++)
          mpOut[x,y] = new List<MusicPoint>();
      return;      
    }
    
    int newWidth = this.level >> 1;
    
    subtrees[0,0].GetAllSongs(mpOut, x, y, newWidth);
    subtrees[0,1].GetAllSongs(mpOut, x, y+newWidth, newWidth);
    subtrees[1,0].GetAllSongs(mpOut, x+newWidth, y, newWidth);
    subtrees[1,1].GetAllSongs(mpOut, x+newWidth, y+newWidth, newWidth);
  }
  
  public float[,] GenerateHeightMap(int targetLevel, bool average){
    
    if(targetLevel < 0)
      targetLevel=0;
    //     else if (targetLevel > this.level)
    //       targetLevel=this.level;
    
    int arraySize = 1 << (this.level - targetLevel);    // works because this quad tree is a 2D binary tree
    
    float[,] heights = new float[arraySize,arraySize];
    for(int i = 0; i < arraySize; i++){
      for(int j = 0; j < arraySize; j++){
        heights[i,j] = 0.0f;
      }
    }
    
    if(targetLevel == this.level || this.subtrees == null){
      heights[0,0] = average ? this.averageHeight : (float)this.datapoints;
      return heights;
    }
    
    int newWidth = arraySize/2;
    
    subtrees[0,0].GenerateHeightMap(targetLevel, heights, 0, 0, newWidth, average);
    subtrees[0,1].GenerateHeightMap(targetLevel, heights, 0, newWidth, newWidth, average);
    subtrees[1,0].GenerateHeightMap(targetLevel, heights, newWidth, 0, newWidth, average);
    subtrees[1,1].GenerateHeightMap(targetLevel, heights, newWidth, newWidth, newWidth, average);
    
    return heights;
  }
  
  private void GenerateHeightMap(int level, float[,] outValues, int x, int y, int width, bool average){
    if ( this.level == level ){
      outValues[x,y] = average ? this.averageHeight : (float)this.datapoints;
      return;
    }
    
    if(subtrees == null)
      return;
    
    int newWidth = width/2;
    
    subtrees[0,0].GenerateHeightMap(level, outValues, x, y, newWidth, average);
    subtrees[0,1].GenerateHeightMap(level, outValues, x, y+newWidth, newWidth, average);
    subtrees[1,0].GenerateHeightMap(level, outValues, x+newWidth, y, newWidth, average);
    subtrees[1,1].GenerateHeightMap(level, outValues, x+newWidth, y+newWidth, newWidth, average);
    
  }
  
  private bool IsPowerOfTwo(int x){
    return (x & (x - 1)) == 0;
  }
  
  public float[,] GenerateHeightMapOnLevel(int startLevel, int desiredWidth, float coord_x, float coord_y, bool average){
    /** startLevel — this is the level we consider to be the top level.
     *  desiredWidth — we want our output to be this wide. Needs to be ^2. TODO: make a check & throw exception
     *  coord_x — we want to get the area around this point
     *  coord_y — we want to get the area around this point
     *  average — are we looking at song density (true) or number of songs (false)
     */
    
    /** TODO: implement proper checks for startlevel and desiredWidth*/
    
    
    return null;
    
  }
  
  
}
