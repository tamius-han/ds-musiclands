using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainChunk {
  public int chunkId;
  public bool tagged;                           // did we attempt to download songs from here?
  public List<MusicPoint> availableSongs;     // songs that cache says are not unavailable
  public List<MusicPoint> allSongs;             // all songs, ordered approximately by proximity to some average point
  
  int lastId = -1;
  int lastAvailableId = -1;
  
  public TerrainChunk(int id){
    this.chunkId = id;
    this.tagged = false;
    this.availableSongs = new List<MusicPoint>();
  }
  
  public static List<MusicPoint> GetOrderedChunkSongsApproximate(TerrainTree terrainTree, int pos_x, int pos_y){
    // Orders songs by how representative of the chunk they are. For chunks with a lot (CHUNK_SIZE²+) of songs
    // This function should run in near-constant time, but very slowly
    
    List<MusicPoint>[,] mparr = new List<MusicPoint>[TerrainInit.CHUNK_SIZE,TerrainInit.CHUNK_SIZE];
    
    terrainTree.GetSongsInArea(TerrainInit.CHUNK_LEVEL, mparr, pos_x*TerrainInit.CHUNK_SIZE, pos_y*TerrainInit.CHUNK_SIZE);
    
    // determine roughly average song position inside the chunk
    float average_x = 0f;
    float average_y = 0f;
    
    for (int ci = 0; ci < TerrainInit.CHUNK_SIZE; ci++) {
      for( int cj = 0; cj < TerrainInit.CHUNK_SIZE; cj++){
        if(mparr[ci,cj] == null){
          mparr[ci,cj] = new List<MusicPoint>();
          continue;
        }
        average_x += ci * mparr[ci,cj].Count;
        average_y += cj * mparr[ci,cj].Count;
      }
    }
    
    average_x /= (float)(TerrainInit.CHUNK_SIZE * TerrainInit.CHUNK_SIZE);
    average_y /= (float)(TerrainInit.CHUNK_SIZE * TerrainInit.CHUNK_SIZE);
    
    // calculate chunk proximity to the average point
    PriorityQueueF<List<MusicPoint>> ordering = new PriorityQueueF<List<MusicPoint>>(false);
    float distance;
    
        for(int ci = 0; ci < TerrainInit.CHUNK_SIZE; ci++){
      for(int cj = 0; cj < TerrainInit.CHUNK_SIZE; cj++){
        distance = ( (ci - average_x) * (ci - average_x) ) + ( (cj - average_y) * (cj - average_y) );
        ordering.Enqueue(mparr[ci,cj], distance);
      }
    }
    
    // add all songs to a single list and return it    
    List<MusicPoint> allSongs = new List<MusicPoint>();
    while(! ordering.IsEmpty())
      allSongs.AddRange(ordering.Dequeue());
    
    
    return allSongs;
  }
  

  public static List<MusicPoint> GetOrderedChunkSongs(TerrainTree terrainTree, int pos_x, int pos_y){
    // Populates chunk with songs. For chunks with a small or moderate (< CHUNK_SIZE²) of songs
    // 
    
    TerrainTree subtree = terrainTree.GetSubtreeAt(TerrainInit.CHUNK_LEVEL, pos_x, pos_y);
    
    if(subtree == null)
      return new List<MusicPoint>();
    
    List<MusicPoint> musicPoints = subtree.GetAllSongs();
    
    
    
    // determine roughly average song position inside the chunk
    float average_x = subtree.average_x;
    float average_y = subtree.average_y;
    
    
    // calculate chunk proximity to the average point
    PriorityQueueF<MusicPoint> ordering = new PriorityQueueF<MusicPoint>();
    float distance;
    
    foreach(MusicPoint mp in musicPoints){
      distance = ( (mp.x - average_x) * (mp.x - average_x) ) + ( (mp.y - average_y) * (mp.y - average_y) );
      ordering.Enqueue(mp, distance);
    }
    
    // add songs to chunk
    
    List<MusicPoint> allSongs = new List<MusicPoint>();
    while(! ordering.IsEmpty())
      allSongs.Add(ordering.Dequeue());
    
    return allSongs;
  }
  
  
  
  /*
   *  public int GetNextSong(){
   *    return allSongs[++lastId];
}*/
  
}
