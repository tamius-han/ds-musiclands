using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Peak {
  public readonly int id;
  public int x;
  public int y;
  List<MusicPoint> allSongs;  // misnomer — there'll be 100 at most (prolly a lot less) — but let's use same terms as TerrainChunk
  public string genre = "";   // Since we won't store genre for all songs (we'll only query GPM for genre of the closest
                              // song), this is okay. We won't prefetch genres of all songs because we don't want to
                              // feel Google's banhammer
  
  public Color baseColor;
  public Color snowColor;
  public Color boxColor;
  
  public Peak(int id, int x, int y){
    this.id = id;
    this.x = x;
    this.y = y;
    
    allSongs = new List<MusicPoint>();
    
    
    // somewhat random base color
    
    float baseHue = ((float)(x)) / (TerrainInit.TERRAIN_SIZE_MAX * 1.20f) - 0.2f;
    float baseValue = ((float)(y)) / (TerrainInit.TERRAIN_SIZE_MAX * 1.8f) - 0.15f;
    
    float hue = Mathf.Clamp( baseHue + ((Random.value - 0.5f)*0.1f), 0.0f, 0.66f);
    float value = Mathf.Clamp( baseValue + ((Random.value - 0.25f) * 0.2f), 0.0f, 0.75f);
    float saturation = Mathf.Min(Random.value, value + 0.85f); // darker the color, less saturated it is ... up to an extent
    
    this.baseColor = Color.HSVToRGB(hue, saturation, value);
    
    value = Mathf.Clamp( baseValue + ((Random.value - 0.25f) * 0.5f), 0.16f, 0.84f);
    saturation = Mathf.Max(0.25f, Mathf.Min(Random.value, value + 0.90f));
    
    this.boxColor = Color.HSVToRGB(hue, saturation, value);
    
    // less random snow color. Blue can vary more in terms of value and saturation
    hue = (hue + ( (Random.value-0.5f) * 0.20f)) % 1f;   
    value = ( (Random.value + Mathf.Max(0.0f, hue - 0.5f)) * 0.25f) + 0.75f;
    saturation = (Random.value + Mathf.Max(-0.5f, hue - 0.5f)) * Mathf.Max(0.20f, (1f - value));
    
    this.snowColor = Color.HSVToRGB(hue, saturation, value);
    
    
  }
  
  public void BuildRelevantSongs(TerrainTree tt){
    // We aren't going to be too thorough with this, we'll just take a look at the terrainTree level 0 within few
    // spaces of our peak.
    
    List<MusicPoint> songs = new List<MusicPoint>();
    
    int x_min, x_max, y_min, y_max;
    
    // +/-1 within the peak should contain the closest songs
    
    x_min = SS.Max(this.x - 1, 0);
    y_min = SS.Max(this.y - 1, 0);
    
    x_max = SS.Min(this.x + 1, TerrainInit.TERRAIN_SIZE_ACTUAL);
    y_max = SS.Min(this.y + 1, TerrainInit.TERRAIN_SIZE_ACTUAL);
    
    for(int i = x_min; i < x_max; i++)
      for(int j = y_min; j < y_max; j++)
        songs.AddRange(tt.GetSongsInArea(0, i, j));
     
    // calculate chunk proximity to the peak point
    PriorityQueueF<MusicPoint> ordering = new PriorityQueueF<MusicPoint>();
    float distance;
    
    foreach(MusicPoint mp in songs){
      distance = (
        ( (mp.x - (float)this.x) * (mp.x - (float)this.x) ) + 
        ( (mp.y - (float)this.y) * (mp.y - (float)this.y) )
      );
      
      ordering.Enqueue(mp, distance);
    }
    
    // add songs to peak, but only some 
    int count = TerrainSettings.PEAK_SONGCOUNT_MAX;
    List<MusicPoint> allSongs = new List<MusicPoint>();
    while(! ordering.IsEmpty() && count --> 0)
      this.allSongs.Add(ordering.Dequeue());
  }
  
  public void GetChunkGenre(){
    // if allSongs == null throw exception, maybe?
    this.genre = CacheOptions.GenreFetchFirst(allSongs);
  }
  
}
