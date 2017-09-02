using System;
using System.IO;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CacheOptions : MonoBehaviour {
 
  /**  ___________________
   *  ||  PUBLIC-FACING  ||
   *  ||    S T U F F    ||
   *   \=================/
   */
  
  /** enums vs. binary
   *
   *
   *bits   0         6         12     ---> 31
   *       | x x x x | x x x x |      ...   |
   *         \_____/   \_____/
   *           |          |
   * cache vs. 7digital   |
   *                      |
   *        info for full clips on GPM, if we ever implement
   */
  
  // 
  protected static int UNKNOWN = 0;
  
  // putting in enum in the class lul
  public static readonly int NOT_CACHED = 1;           // Not in our cache, no telling if 7digital has it or not
  public static readonly int DOWNLOADING = 2;          // Not in our cache, but we're working on getting a 30sec preview
  public static readonly int DOWNLOADED = 4;           // 30sec preview is in our cache, in .mp3
  public static readonly int PLAYABLE = 8;             // 30sec preview is converted to a format we can play
  public static readonly int NOT_AVAILABLE = 16;       // 7digital doesn't have it
  public static readonly int STATUS_7DIGITAL = 31;     // bits containing 7digital statuses
  
  // GPM has similar, but different statuses.
  public static readonly int GPM_NOT_CACHED = 32;      // full version not cached and untried
  public static readonly int GPM_READY = 64;           // we know this song exists on GPM
  public static readonly int GPM_STREAMING = 128;      // we are currently streaming that song
  public static readonly int GPM_CACHED = 256;         // full song is in our cache
  public static readonly int GPM_NOT_AVAILABLE = 512;  // GPM doesn't have it
  public static readonly int STATUS_GPM = 992;         // bits containing GPM statuses
  
  /**
   *    _________________________
   *   ||    I N T E R N A L    ||
   *   ||       S T U F F       ||
   *    \=======================/
   * 
   */
  
  static object _writeLock = new object();              // UpdateCachedItems needs to be threadsafe when writing to file 
  static object _cacheLock = new object();              // when adding new cache entry too, actually (but not when modifying)
  
  // File for saving unavailable tracks permanently/until manual deletion
  static string PERMANENT_FILE_UNAVAILABLE = Application.dataPath + "/Resources/store/cache_unavailable";
  
  // we keep track of which songs we're downloading and which songs are downloaded via dictionary.
  static Dictionary<int, CachedInfo> cachedItems;
  
  // this is a 401 counter. When this value reaches zero, we don't attempt any downloads from 7digital until reset manually
  static int fuse_7digital = 5;
  static readonly int fuse_7digital_default = 5; // default number of 401 retries
  
  
  static string cachedir = "";
  static ThreadsafeQueue<int> downloadQueue = new ThreadsafeQueue<int>();
  
  /**
   *    _______________________
   *   ||   I N T E R N A L   ||
   *   ||      FUNCTIONS      ||
   *    \=====================/
   * 
   */
  
  static void AppendUnavailableItem(int id){    
    // FileMode.Append creates a new file, so no need to check if file exists. Only one thread at a time pls
    lock(_writeLock){
      using (BinaryWriter writer = new BinaryWriter(File.Open(PERMANENT_FILE_UNAVAILABLE, FileMode.Append))){
        writer.Write(id);
      }
    }
  }
  
  static void LoadUnavailableItems(){  // loads ids that are unavailable
    if(File.Exists(PERMANENT_FILE_UNAVAILABLE)){
      int id;
      using (BinaryReader reader = new BinaryReader(File.Open(PERMANENT_FILE_UNAVAILABLE, FileMode.Open))){
        while(reader.BaseStream.Position != reader.BaseStream.Length){
          id = reader.ReadInt32();
          InitiateCachedItems(id, NOT_AVAILABLE, STATUS_7DIGITAL);
        }
      }
    }
  }
  
  static int DownloadItem(int id, string provider){
    int exitStatus;
    if(provider == "7digital"){
      SdigitalKey key = SdigitalConf.GetKeySecret();
      exitStatus = Python.RunScript(
        "7digital-dl.py", (
          "--key " + key.key +
          " --secret " + key.secret +
          " -s " + id
        )
      );
      
      if (exitStatus == 41){
        UpdateCachedItems(id, NOT_CACHED, STATUS_7DIGITAL);
      }
      else if (exitStatus != 0){
        UpdateCachedItems(id, NOT_AVAILABLE, STATUS_7DIGITAL);
      }
      else
        UpdateCachedItems(id, DOWNLOADED, STATUS_7DIGITAL);
      
      return exitStatus;
    }
    if(provider == "gpm"){
      return -1;
    }
    
    return -1;
  }
  
  
  
  static void UpdateCachedItems(int id, int status, int status_provider){
    if(status == NOT_AVAILABLE){
      AppendUnavailableItem(id);
    }
    
    if(cachedItems.ContainsKey(id)){    
      int ncs = cachedItems[id].cacheStatus;   // get current status
      ncs &= ~status_provider;                 // reset [provider]'s bits
      ncs |= status;                           // raise [stauts] bit
      cachedItems[id].cacheStatus = ncs;       // put new status back in
    }
    else{
      CachedInfo ci = new CachedInfo();
      ci.cacheStatus = status;
      lock(_cacheLock){
        cachedItems.Add(id,ci);
      }
    }
  }
  
  static void InitiateCachedItems(int id, int status, int status_provider){
    if(cachedItems.ContainsKey(id)){    
      int ncs = cachedItems[id].cacheStatus;   // get current status
      ncs &= ~status_provider;                 // reset [provider]'s bits
      ncs |= status;                           // raise [stauts] bit
      cachedItems[id].cacheStatus = ncs;       // put new status back in
    }
    else{
      CachedInfo ci = new CachedInfo();
      ci.cacheStatus = status;
      cachedItems.Add(id,ci);
    }
  }
  
  
  /**
   *    _______________________
   *   ||    PUBLIC-FACING    ||
   *   ||      FUNCTIONS      ||
   *    \=====================/
   * 
   */
  
  
  public static void Init(){
    GlobalData.dataPath = Application.dataPath;
    
    
    cachedItems = new Dictionary<int,CachedInfo>();
    cachedir = Application.dataPath + "/Resources/cache";
    
    GlobalData.cachedir = cachedir;
    
    if(! Directory.Exists(cachedir) )
      Directory.CreateDirectory(cachedir);
    
    if(! Directory.Exists(cachedir + "/clips"))
      Directory.CreateDirectory(cachedir + "/clips");
    
    LoadUnavailableItems();
    
    var filesInCache = Directory.GetFiles(cachedir + "/clips");
    for (int i = 0; i < filesInCache.Length; i++) {
      try{
        int id = int.Parse(Path.GetFileNameWithoutExtension(filesInCache[i]));
        UpdateCachedItems(id, DOWNLOADED, STATUS_7DIGITAL);
      }
      catch(Exception e){ }
    }
  }
  
 
  
  public static void ResetFuse(string provider){
    if(provider == "7digital")
      fuse_7digital = fuse_7digital_default;
  }
  
  public static void ClearCache(){
    string cachedir = Application.dataPath + "/Resources/cache";
    
    if( Directory.Exists(cachedir) ){
      Directory.Delete(cachedir, true);
    }
    
    Directory.CreateDirectory(cachedir);
    Directory.CreateDirectory(cachedir + "/dl");
    Directory.CreateDirectory(cachedir + "/clips");
    Directory.CreateDirectory(cachedir + "/full");
  }
  
  public static void DeleteCacheItem(int id){
    
    File.Delete(Application.dataPath + "/Resources/cache/clips" + id + ".mp3");
//     File.Delete(Application.dataPath + "/Resources/cache/full" + id + ".mp3");
    
    // remove from dictionary, too
    cachedItems.Remove(id);
  }
  
  public static int FindItemStatus(int id){
    if(! cachedItems.ContainsKey(id))
      return NOT_CACHED | GPM_NOT_CACHED;
    
    return cachedItems[id].cacheStatus;
  }
  
  public static void FetchItem(int id, string provider){
    if(provider == "7digital"){
      bool newCacheInfoEntry = true;
      
      if(cachedItems.ContainsKey(id)){
        CachedInfo ci = cachedItems[id];
        if( (ci.cacheStatus & STATUS_7DIGITAL) == DOWNLOADING || (ci.cacheStatus & STATUS_7DIGITAL) == DOWNLOADED )
          return;
        
        newCacheInfoEntry = false;
      }
      
      // now that we determined whether we need a new dictionary entry or not:
      if(newCacheInfoEntry){
        CachedInfo ci = new CachedInfo();
        ci.cacheStatus = DOWNLOADING;
        cachedItems.Add(id,ci);
      }
      else{
        UpdateCachedItems(id, DOWNLOADING, STATUS_7DIGITAL);
      }
      
    }
  if(provider == "gpm"){
    //todo: GPM download  
   }
  
//   print("start downloading?");
  // start downloading in a background thread      
  Thread t = new Thread(
    () => DownloadItem(id, provider));
  t.Start();
  
  }
  
//   static int maxMessages = 50;
  
  public static void FetchFirstN(TerrainChunk chunk, int n, string provider){
    int exitStatus, itemStatus;
    int nonfailed = 0;
    
//     print("fetching first " + n + " songs from chunk " + chunk.chunkId + ". This chunk has " + chunk.allSongs.Count + " songs in total.");
    
    if(provider == "7digital"){
      if(fuse_7digital <= 0)
        return;
      foreach(MusicPoint mp in chunk.allSongs){
        itemStatus = FindItemStatus(mp.id) & STATUS_7DIGITAL;
        
//         if(maxMessages --> 0)
//           print("item " + mp.id + " has cache status " + itemStatus);
        
        if( (itemStatus & NOT_AVAILABLE) != 0) //skip unavailable
          continue;
        
        if( (itemStatus & NOT_CACHED) == 0){   // if song is downloaded, downloading or playable, it should already be on
          nonfailed++;                         // chunk's 'availableSongs' list. Let's not do anything.  
          if(nonfailed >= n)
            break;
          continue;
        }
        
        // if we come to here, we're looking at uncached song we haven't attempted to download. inb4 download
        
//         print("attempting to download item " + mp.id);
        
        exitStatus = DownloadItem(mp.id, provider);
        
        if(exitStatus == 41){
          --fuse_7digital;
          print("We got a 401 from " + provider + "? — retries left before giving up: " + fuse_7digital);
          if(fuse_7digital <= 0)
            return;
        }
        
        
        if(exitStatus == 0){
          chunk.availableSongs.Add(mp);
          
          nonfailed++;
          if(nonfailed >= n)
            break;
        }
      }
    }
  }
  
  public static void FetchFirstN(List<MusicPoint> mp, int n, string provider, int[] musicIdOut, bool background){
    if(background){
      Thread t = new Thread(
        () => FetchFirstN_bg(mp, n, provider, musicIdOut));
      t.Start();
    }
    else{
      FetchFirstN_bg(mp, n, provider, musicIdOut);
    }
  }
  
  // FetchFirstN should attempt downloads sequentially
  static void FetchFirstN_bg(List<MusicPoint> musicPoints, int n, string provider, int[] musicIdOut){
    int exitStatus;
    int nonfailed = 0;
    
    for(int i = 0; i < musicIdOut.Length; i++){
      musicIdOut[i] = -1;
    }
    
    if(provider == "7digital"){
      foreach(MusicPoint mp in musicPoints){
        if( (FindItemStatus(mp.id) & STATUS_7DIGITAL & ~NOT_CACHED) != 0 ){
          // no redownloads
          musicIdOut[nonfailed] = mp.id;
          //todo: if cache would actually populate at the beginning, this wouldn't be necessary
          UpdateCachedItems(mp.id, DOWNLOADED, STATUS_7DIGITAL);
          //end todo          
          nonfailed++;
          if(nonfailed >= n)
            break;
          continue;
        }
        
//         print("attempting to download song with id " + mp.id);
        
        exitStatus = DownloadItem(mp.id, provider);
        if(exitStatus == 0){
          musicIdOut[nonfailed] = mp.id;
          nonfailed++;
          
          if(nonfailed >= n)
            break;
        }
      }
    }
    
  }
  
  
}

class CachedInfo {
  public int cacheStatus;
}
