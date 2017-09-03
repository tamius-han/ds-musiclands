using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(GameObject))]

public class BasicRadioCtl : MonoBehaviour {

  public static int PREFETCH_7DIGITAL = 1;
  public static int PREFETCH_GPM = 3;
  public static string PROVIDER_PREFERENCE = "gpm";
  public static int GPM_WAIT_FOR_FIND_TIMEOUT = 2000;
  public static int EDGE_TRIGGER_RESET_DELAY = 1500; //msec
  public static int NO_TRIGGER_PERIOD = 3000; //msec
  
  public AudioSource musicPlayer;
  
  static TerrainChunk[,] chunks;
//   static GameObject player;
  
  static ThreadsafeQueue<int> musicQueue;
  
  static float chunkFraction;
  static bool initialized = false;
  int lastChunk_x, lastChunk_y, chunkTag;
  
  
  
  
  
  /** non-static stuff **/
  
  Text stationText, songInfoText;
  
  
  /** from editor **/
  
  public GameObject player;
  
  /** **/
  
  public static void Init(){
    
//     musicPlayer = GameObject.FindSceneObjectsOfType(typeof(AudioSource))[0] as AudioSource;
    
    if(musicQueue == null)
      musicQueue = new ThreadsafeQueue<int>();
    
    
    musicQueue.Drop(); // chunk tags won't work on re-initialization because we go one level down
    
    initialized = true;
  }
  
  public static void LoadChunksAroundPC(TerrainTree tt, int range){
    
//     float playerX = player.transform.position.x;
//     float playerY = player.transform.position.z;
    
//     for(int i = -range; i <= range; i++){
//       for(int j = -range; j <= range; j++){
//         List<MusicPoint> mp = tt.GetSongsInArea(5, playerX + ( 32.0f * (float)i), playerY + ( 32.0f * (float)j));
//         int[] ready_ids = new int[5];
//         print("attempting to fetch first 2-something songs from current cell. TerrainTree has " + mp.Count + " songs" + ( (i == 0 && j == 0)?" [player chunk !!]":"") );
//         CacheOptions.FetchFirstN(mp, 2, "7digital", ready_ids);
//       }
//     }
    
  }
  
  
  
  
  
  /*** internal static ***/
  
  static int max(int a, int b){
    return a>b?a:b;
  }
  static int min(int a, int b){
    return a<b?a:b;
  }
  
  /*** internal non-static ***/
  
  void SetSongInfo(string needsParsing){
    string[] shittyparsed = needsParsing.Split(new string[] {" - "}, 2, System.StringSplitOptions.None);
    
    SetSongInfo(shittyparsed[0], shittyparsed[1]);
  }
  
  void SetSongInfo(string artist, string title){
    songInfoText.text = "<size=\"16\"><color=\"#bbbbbb\">" + artist + 
                        "</color></size>\n<size=\"24\"><b>" + title + "</b></size>";
  }
  

  public static void LoadInitialTracksGpm(int howMany, int radius, float player_x, float player_y){
    print("<GPM LOAD>       [BasicRadioCtl::LoadInitialTracksGPM] attempting to load " + howMany + " tracks within " + radius + " chunks of player before we allow exploring");
    
    int chunk_x = (int)Mathf.Floor(player_x / TerrainInit.CHUNK_SIZE);
    int chunk_y = (int)Mathf.Floor(player_y / TerrainInit.CHUNK_SIZE);
    
    int minChunk_x, maxChunk_x, minChunk_y, maxChunk_y, numOfChunks;
    
    numOfChunks = GlobalData.chunks.GetLength(0);
    
    minChunk_x = max(0, (chunk_x - radius));
    minChunk_y = max(0, (chunk_y - radius));
    
    maxChunk_x = min((chunk_x + radius), (numOfChunks - 1));
    maxChunk_y = min((chunk_y + radius), (numOfChunks - 1));
    
//     GlobalData.songFetching_total = (maxChunk_x - minChunk_x + 1) * (maxChunk_y - minChunk_y + 1);
//     GlobalData.songFetching_completed = 0;
    
    object _threadCountLock = new object();
    int threads = GlobalData.maxConcurrentDownloads;
    Thread t;
    if( chunk_x >= minChunk_x && 
      chunk_x <= maxChunk_x && 
      chunk_y >= minChunk_y &&
      chunk_y <= maxChunk_y ){
        CacheOptions.FetchFirstN( GlobalData.chunks[chunk_x, chunk_y], howMany, "gpm");
      }
      
      for(int i = minChunk_x; i <= maxChunk_x; i++){
        for(int j = minChunk_y; j <= maxChunk_y; j++){
          CacheOptions.FetchFirstN(GlobalData.chunks[i,j], howMany, "gpm");
        }
      }
  }
  
  public static void LoadInitialTracks(int howMany, int radius, float player_x, float player_y){
    
    print("attempting to load " + howMany + " tracks for chunks within " + radius + " chunks of player before we allow exporing");
    
    int chunk_x = (int)Mathf.Floor(player_x / TerrainInit.CHUNK_SIZE);
    int chunk_y = (int)Mathf.Floor(player_y / TerrainInit.CHUNK_SIZE);
    
    int minChunk_x, maxChunk_x, minChunk_y, maxChunk_y, numOfChunks;
    
    numOfChunks = GlobalData.chunks.GetLength(0);
    
    minChunk_x = (chunk_x - radius <= 0) ? 0 : (chunk_x - radius);
    minChunk_y = (chunk_y - radius <= 0) ? 0 : (chunk_y - radius);
    
    maxChunk_x = (chunk_x + radius < numOfChunks) ? (chunk_x + radius) : numOfChunks - 1;
    maxChunk_y = (chunk_y + radius < numOfChunks) ? (chunk_y + radius) : numOfChunks - 1;
    
    GlobalData.songFetching_total = (maxChunk_x - minChunk_x + 1) * (maxChunk_y - minChunk_y + 1);
    GlobalData.songFetching_completed = 0;
    
    
    print("in total, we'll fetch from " + GlobalData.songFetching_total + " chunks");
    
    object _threadCountLock = new object();
    int threads = GlobalData.maxConcurrentDownloads;
    Thread t;
    if( chunk_x >= minChunk_x && 
        chunk_x <= maxChunk_x && 
        chunk_y >= minChunk_y &&
        chunk_y <= maxChunk_y ){
      
      t = new Thread(
        () => CacheOptions.FetchFirstN( GlobalData.chunks[chunk_x, chunk_y], howMany, "7digital")
                     );
      t.Start();
      
      GlobalData.songFetching_completed++;
    }

    for(int i = minChunk_x; i <= maxChunk_x; i++){
      for(int j = minChunk_y; j <= maxChunk_y; j++){
        while(threads <= 0){
          System.Threading.Thread.Sleep(10);
        }
        lock(_threadCountLock){
          --threads;
        }
        
        t = new Thread(
          () => {
            CacheOptions.FetchFirstN( GlobalData.chunks[i, j], howMany, "7digital");
            lock(_threadCountLock){
              GlobalData.songFetching_completed++;
              ++threads;
            }
          }
        );
        t.Start();
      }
    }
    
  }
  
  static void PrefetchChunkInitialTracks(AreaBounds ab, int chunk_x, int chunk_y){
    // todo: indicator that we're fetching tracks in the background

//     Thread sd  = new Thread( () => { Prefetch7digital(ab, chunk_x, chunk_y); print("7digital prefetched"); } );
    Thread gpm = new Thread( () => { PrefetchGpm(ab, chunk_x, chunk_y); print("gpm prefetched"); } );
//     sd.Start();
    gpm.Start();
    
    
    // todo: when loading is done, indicate that we're no longer fetching tracks
  }
  
  static void Prefetch7digital(AreaBounds ab, int chunk_x, int chunk_y){
    for(int i = ab.xmin; i < ab.xmax; i++){
      for(int j = ab.ymin; j < ab.ymax; j++) {
        if(i == chunk_x && j == chunk_y)
          continue;
        
        CacheOptions.FetchFirstN( GlobalData.chunks[i, j], PREFETCH_7DIGITAL, "7digital");
      }
    }
  }
  
  static void PrefetchGpm(AreaBounds ab, int chunk_x, int chunk_y){
    for(int i = ab.xmin; i < ab.xmax; i++){
      for(int j = ab.ymin; j < ab.ymax; j++) {
        if(i == chunk_x && j == chunk_y)
          continue;
        
        CacheOptions.FetchNextN( GlobalData.chunks[i, j], PREFETCH_GPM, "gpm");
      }
    }
  }
  
  void PrefetchGpm_nodl(int i, int j){
    print("<PrefetchGpm_nodl> starting no-dl prefetch!");
    // prefetch-only, no downloads
    if(GlobalData.chunks[i,j].currentId >= GlobalData.chunks[i,j].lastFetched_gpm){
      CacheOptions.FetchNextN( GlobalData.chunks[i,j], PREFETCH_GPM, "gpm");
    }
  }
  
  void PrefetchGpm_nodl(TerrainChunk chunk){
    print("<PrefetchGpm_nodl> starting no-dl prefetch!");
    // prefetch-only, no downloads
    if(chunk.currentId >= chunk.lastFetched_gpm){
      CacheOptions.FetchNextN( chunk, PREFETCH_GPM, "gpm");
    }
  }
  
  
  IEnumerator PrefetchGpm_dl(int i, int j){
    // prefetch if needed
    print("<PrefetchGpm_dl> starting prefetch!");
    if(GlobalData.chunks[i,j].currentId >= GlobalData.chunks[i,j].lastFetched_gpm){
      Thread t = new Thread( () => CacheOptions.FetchNextN( GlobalData.chunks[i,j], PREFETCH_GPM, "gpm") );
      t.Start();
      while(t.IsAlive)
        yield return null;
    }
    // start downloading in background
    StartCoroutine(GpmDl(GlobalData.chunks[i,j]));
    yield return null;
  }
  
  IEnumerator GpmDl(TerrainChunk chunk){
    
    print("<GpmDl> Starting prefetch prefetch!");
    
    List<MusicPoint> mplist = chunk.allSongs;
    
    if(mplist.Count <= 0)
      yield break; // pointless if no songs
    
    MusicPoint nowPlay = null;
    MusicPoint mp;
    
    int allowedStatuses = CacheOptions.GPM_READY | CacheOptions.GPM_STREAMING | CacheOptions.GPM_CACHED;
    
    int chunkSongIterator = 0;
    
    restartpls:
    if(chunkSongIterator >= mplist.Count)
      yield break;
    if( chunk.currentId >= mplist.Count) // reset when reaching the end
      chunk.currentId = 0;
    
    while(chunkSongIterator < mplist.Count){
      mp = mplist[ (chunk.currentId + chunkSongIterator) % mplist.Count ];   // we can do up to one full circle
      
      if( (CacheOptions.FindItemStatus(mp.id) & allowedStatuses) != 0 ){
        chunk.currentId = (chunk.currentId + chunkSongIterator) % mplist.Count;   // skip unready chunks for the next time
        nowPlay = mp;
        break;
      }
      
      chunkSongIterator++;
    }
    if(nowPlay == null)
      yield break;
    
    // wait to get stream URL and start download asap
    string streamUrl = "";
    Thread gsu = new Thread( () => streamUrl = GpmConf.GetStream(nowPlay.gpmId) );
    gsu.Start();
    
    while(gsu.IsAlive)
      yield return null;   
    
    print("<GpmDl> We got stream url, starting with download");
    
    Thread t = new Thread( () => CacheOptions.StartGpmStream(nowPlay.id, streamUrl) );
    t.Start();
  }
  
  IEnumerator PlayChunkTrackWhenReady(TerrainChunk chunk){
    
    SetSongInfo("fetching chunk songs","");
    Thread prefetch = new Thread( () => PrefetchGpm_nodl(chunk)); // only runs if necessary.
    while(prefetch.IsAlive)
      yield return null;
    
    List<MusicPoint> mplist = chunk.allSongs;
    
    if(mplist.Count <= 0){
      SetSongInfo("No songs here.","");
      yield break; // pointless if no songs
    }
    
    MusicPoint nowPlay = null;
    MusicPoint mp;
    
    int allowedStatuses = CacheOptions.GPM_READY | CacheOptions.GPM_STREAMING | CacheOptions.GPM_CACHED;
    int chunkSongIterator = 0;
    
    restartpls:
    if( chunk.currentId >= chunk.lastFetched_gpm ) // reset when reaching the end of fetched
      chunk.currentId = 0;
    
    chunkSongIterator = chunk.currentId;
    
    while( chunkSongIterator < chunk.lastFetched_gpm){
      mp = mplist[chunkSongIterator];   // we check as far as we fetched. No further
      

      if( (CacheOptions.FindItemStatus(mp.id) & allowedStatuses) != 0 ){
        chunk.currentId = chunkSongIterator;   // skip unready chunks for the next time
        nowPlay = mp;
        break;
      }
      
      chunkSongIterator++;
    }
    
    if(nowPlay == null){             // nothing to do if we haven't found a suitable id
      if(chunk.allSongs.Count == 0){
        SetSongInfo("«no songs to be found in these parts»","");
        yield break;
      }
      else{
        SetSongInfo("<color=\"#99f\">«we got no songs from gpm»</color>","Find item status: " +CacheOptions.FindItemStatus( mplist[ chunkSongIterator - 1 ].id ) );
        yield break;  // todo: refresh if chunk hasn't changed or something
      }
    }
    
    // Let's wait until our song becomes available. That shouldn't take long
    allowedStatuses = /*CacheOptions.DOWNLOADED | CacheOptions.PLAYABLE | */ CacheOptions.GPM_READY  | CacheOptions.GPM_STREAMING | CacheOptions.GPM_CACHED;
                      
    SetSongInfo("<color=\"f00\">«Slow down! Fetch can't catch you.»</color>","<color=\"#f61\">«you move too fast on the space, [Fetch()] don't chase you»</color>");
    while( (CacheOptions.FindItemStatus(nowPlay.id) & allowedStatuses) == 0 ){
      yield return null;
    }
    SetSongInfo("«catching up»","");
    yield return null;
    int status = CacheOptions.FindItemStatus(nowPlay.id);
    
    // we came this far, song should be either downloaded 7digital or ready to be streamed from gpm (at the very worst)
    
//     // If song is available from 7digital, we play the 7digital one by default (7digital is guaranteed to play the exact
//     // song it's supposed to, GPM does best guess based on metadata we have (artist - title —> not much))
//     if(PROVIDER_PREFERENCE == "7digital"){
//       if( (status & CacheOptions.PLAYABLE) != 0){
//         // good, is playable. Let's play the clip thxbai
//         StartCoroutine(PlayClip(nowPlay));
//         yield break;
//       }
//       
//       // must be non-playable, then.
//       if( (status & CacheOptions.DOWNLOADED) != 0){
//         Thread t = new Thread(
//           () => FFmpeg.Convert(GlobalData.cachedir + "/clips/" + nowPlay.id + ".mp3", GlobalData.cachedir + "/clips/" + nowPlay.id + ".wav"));
//         t.Start();
//         
//         while(t.IsAlive)
//           yield return null;
//         
//         StartCoroutine(PlayClip(nowPlay));
//         
//         yield break;
//       }
//       
//       // if we're here, then song is only available from GPM
//     }
    
    if( (status & (CacheOptions.GPM_CACHED | CacheOptions.GPM_STREAMING)) != 0 ){
      // available from our GMP cache!
      StartCoroutine(PlayFull(nowPlay));
      yield break;
    }
    
    int yieldCounter = 0;
    
    if( (status & CacheOptions.GPM_READY ) != 0){
      SetSongInfo("«starting to stream from GPM»","");
      yield return null;
      // start streaming from GPM
      
      print("gpm id of the song is {" + nowPlay.gpmId + "} — getting stream URL");
      
      string streamUrl = "";
      Thread gsu = new Thread( () => streamUrl = GpmConf.GetStream(nowPlay.gpmId) );
      gsu.Start();
      
      while(gsu.IsAlive)
        yield return null;
      
      
      print("got stream URL: " + streamUrl);
      
      Thread t = new Thread( () => CacheOptions.StartGpmStream(nowPlay.id, streamUrl) );
      t.Start();
      
      Stopwatch sw = Stopwatch.StartNew();
      while(! File.Exists(GlobalData.cachedir + "/full/" + nowPlay.id + ".ogg") ){ 
        // wait until ffmpeg starts downloading
        
        if(sw.ElapsedMilliseconds < 1000) // Wait at least 0.5 sec for file to appear
          yield return null;
        else{
          sw.Stop();
          print("does file ["+ GlobalData.cachedir + "/full/" + nowPlay.id + ".ogg" +"] exist? " + File.Exists(GlobalData.cachedir + "/full/" + nowPlay.id + ".ogg"));
//           goto restartpls;
          yield break;
        }
      }
      System.Threading.Thread.Sleep(500); // 500ms should be enough buffer
      
      StartCoroutine(PlayFull(nowPlay));
    }
    
    
//     if(PROVIDER_PREFERENCE != "7digital"){
//       if( (status & CacheOptions.PLAYABLE) != 0){
//         // good, is playable. Let's play the clip thxbai
//         StartCoroutine(PlayClip(nowPlay));
//         yield break;
//       }
//       // must be non-playable, then.
//       Thread t = new Thread(
//         () => FFmpeg.Convert(GlobalData.cachedir + "/clips/" + nowPlay.id + ".mp3", GlobalData.cachedir + "/clips/" + nowPlay.id + ".wav"));
//       t.Start();
//       
//       while(t.IsAlive)
//         yield return null;
//       
//       StartCoroutine(PlayClip(nowPlay));
//     }
  }
  
  
  IEnumerator PlayClip(MusicPoint mp){
    // we assume we already converted to ffmpeg.
    
    string path = GlobalData.cachedir + "/clips/" + mp.id + ".wav";
    
    WWW www = new WWW("file://" + path);
//     print("loading " + path);
    
    AudioClip clip = www.GetAudioClip(false);
    while(!clip.isReadyToPlay){
      yield return www;
    }
    
    clip.name = "7d." + mp.id;
    musicPlayer.clip = clip;
    musicPlayer.Play();
    _update_currentlyPlaying = mp;
  }
  
  IEnumerator PlayFull(MusicPoint mp){
    int waitForLoadstate = 20;
    int loadStateAttempts = 8;
    int i;
    
    retry:
    i = waitForLoadstate;
    
    string path = GlobalData.cachedir + "/full/" + mp.id + ".ogg";
    WWW www = new WWW("file://" + path);
    
    AudioClip clip = www.GetAudioClip(false, true);
    while(clip.loadState == AudioDataLoadState.Unloaded){
      // the null lance: "once zero kills i"
      if(i --<= 0){
        print("[BasicRadioCtl::PlayFull()] whoopsie the loadstate didn't get out of unloaded for too long. retrying");
        
        if( loadStateAttempts --> 0){
          print("[BasicRadioCtl::PlayFull()] retrying to load the clip");
          goto retry;
        }
        else{
          print("[BasicRadioCtl::PlayFull()] we ran out of tries. Let's abandong this folly");
          SetSongInfo(mp.meta, "<color=\"#f00\">Unable to get satisfactory loadstate. Quitting for this song</color>");
          yield return null;
          yield break;
        }
      }
      SetSongInfo(mp.meta, "Clip present but not ready to play");
      
      yield return www;
    }
    SetSongInfo("<color=\"#f60\">[gpm] </color>" +mp.meta);
//     yield return null;
    
    clip.name = "gpm." + mp.id;
    musicPlayer.clip = clip;
    musicPlayer.Play();
    _update_currentlyPlaying = mp;
  }
  
  IEnumerator ClipReloader(MusicPoint mp){
    int waitForLoadstate = 30;
    int status = CacheOptions.FindItemStatus(mp.id);
    
    if( ( PROVIDER_PREFERENCE == "7digital" && (status & (CacheOptions.DOWNLOADED | CacheOptions.PLAYABLE )) != 0)  ||
        ( PROVIDER_PREFERENCE == "gpm" && 
          ( ( status & CacheOptions.GPM_NOT_AVAILABLE) != 0  ) &&
          ( ( status & (CacheOptions.DOWNLOADED | CacheOptions.PLAYABLE) ) != 0 )
        ) ){
      // if we're here, this means we're playing a 7digital clip
      // we aren't streaming 7digital clips, so no need to reload them
      yield break;
    }
    
    // that leaves us with GPM clips
    
    if( musicPlayer.clip.loadState == AudioDataLoadState.Unloaded ) // we won't even try if that's the case
      yield break;
    
    // that leaves us with GPM clips that play
    string path = GlobalData.cachedir + "/full/" + mp.id + ".ogg";
    WWW www = new WWW("file://" + path);
    
    AudioClip newClip = www.GetAudioClip(false, true);

    while(newClip.loadState == AudioDataLoadState.Unloaded){
      if(waitForLoadstate --<= 0){
        print("[BasicRadioCtl::ClipReloader()] whoopsie the loadstate didn't get out of unloaded for too long. not even trying");
        yield break;
      }
      yield return www;
    }
    newClip.name = "gpm." + mp.id;
    
    // name must also match. If the names don't match, the clip was changed during the coroutine, so we don't do it.
    if ( musicPlayer.clip.length != newClip.length && musicPlayer.clip.name == newClip.name ) {
      print("[BasicRadioCtl::ClipReloader()] Reloading the clip!                                      <><>");
      
      int currentTime = musicPlayer.timeSamples;
      musicPlayer.clip = newClip;
      musicPlayer.timeSamples = currentTime;
      musicPlayer.Play();
    }
  }
  
  // Use this for initialization
  void Start () {
    if(musicQueue == null)
      musicQueue = new ThreadsafeQueue<int>();
    
    int numOfChunks = TerrainInit.TERRAIN_SIZE_ACTUAL / TerrainInit.CHUNK_SIZE;
    chunks = new TerrainChunk[numOfChunks,numOfChunks];
    musicPlayer = GameObject.FindSceneObjectsOfType(typeof(AudioSource))[0] as AudioSource;
    
    lastChunk_x = 0;
    lastChunk_y = 0;
    
    stationText = GameObject.Find("StationInfo").GetComponent<Text>();
    songInfoText = GameObject.Find("SongInfo").GetComponent<Text>();
    
    lastTriggered = Stopwatch.StartNew();
    chunkChange = Stopwatch.StartNew();
  }
  
  // global variables for function update
  
  MusicPoint _update_currentlyPlaying = null;
  
  int _update_clipCheckPeriod = 120; // frames
  int _update_ccpFrameCounter = 0;  // frame counter for above
  bool edgeTriggered = false;
  int edgeTriggeredResetDelay = 1000; // msec
  int framesSinceChunkChange = 0;
  Stopwatch lastTriggered;
  Stopwatch chunkChange;
  
  // Update is called once per frame
  void Update () {
    if( ! (initialized && GlobalData.playerHasControl) )
      return;
    if( GlobalData.bottomLevel )
      return;
    
    if( ! player.activeSelf ){
      musicPlayer.Stop();
      return;
    }
    
    ++framesSinceChunkChange;
    
    // which chunk are we in?
    // TODO: which peak is the closest to us
    
    float playerX = player.transform.position.x;
    float playerY = player.transform.position.z;
    
    int chunk_x = ((int)playerX) / TerrainInit.CHUNK_SIZE;
    int chunk_y = ((int)playerY) / TerrainInit.CHUNK_SIZE;
    
    int chunkOffset_x = ((int)playerX) % TerrainInit.CHUNK_SIZE;
    int chunkOffset_y = ((int)playerY) % TerrainInit.CHUNK_SIZE;
    
    if(Input.GetKeyDown(KeyCode.C)){
      Voronoish.GenerateVoronoiDebug((int)playerX, (int)playerY, GlobalData.peaks);
    }
    
    int treshold_upper = TerrainInit.CHUNK_SIZE - TerrainSettings.GPM_PREFETCH_TRESHOLD;
    
  // note: can't retrigger too soon
    if(! edgeTriggered && lastTriggered.ElapsedMilliseconds > NO_TRIGGER_PERIOD){
      edgeTriggered = true;
      
      // handle corners first
      // in this case, we just prefetch all three possible options, but don't start the actual downloads
      // actually don't even check for diagonals
//       if( chunkOffset_x <= TerrainSettings.GPM_PREFETCH_TRESHOLD &&
//           chunkOffset_y <= TerrainSettings.GPM_PREFETCH_TRESHOLD){
//         Thread t = new Thread(
//           () => PrefetchGpm_nodl( SS.Max(0, chunk_x - 1), SS.Max(0, chunk_y - 1) )
//         );
//         t.Start();
//         Thread u = new Thread(
//           () => PrefetchGpm_nodl( SS.Max(0, chunk_x - 1), chunk_y ) // chunk immediately back
//         );
//         u.Start();
//         Thread w = new Thread(
//           () => PrefetchGpm_nodl( chunk_x , SS.Max(0, chunk_y - 1) ) // chunk immediately under
//         );
//         w.Start();
//       }
//       else if ( chunkOffset_x <= TerrainSettings.GPM_PREFETCH_TRESHOLD &&
//                 chunkOffset_y >= treshold_upper ){
//         Thread t = new Thread(
//           () => PrefetchGpm_nodl( SS.Max(0, chunk_x - 1), SS.Min(TerrainInit.NUMBER_OF_CHUNKS, chunk_y + 1) )
//         );
//         t.Start();
//         Thread u = new Thread(
//           () => PrefetchGpm_nodl( SS.Max(0, chunk_x - 1), chunk_y  )
//         );
//         u.Start();
//         Thread w = new Thread(
//           () => PrefetchGpm_nodl( chunk_x, SS.Min(TerrainInit.NUMBER_OF_CHUNKS, chunk_y + 1)  )
//         );
//         w.Start();
//       }
//       else if ( chunkOffset_x >= treshold_upper &&
//                 chunkOffset_y <=  TerrainSettings.GPM_PREFETCH_TRESHOLD){
//         Thread t = new Thread(
//           () => PrefetchGpm_nodl( SS.Min(TerrainInit.NUMBER_OF_CHUNKS, chunk_x + 1), SS.Max(0, chunk_y - 1) )
//         );
//         t.Start();
//         Thread u = new Thread(
//           () => PrefetchGpm_nodl( SS.Min(TerrainInit.NUMBER_OF_CHUNKS, chunk_x + 1), chunk_y )
//         );
//         u.Start();
//         Thread w = new Thread(
//           () => PrefetchGpm_nodl( chunk_x, SS.Max(0, chunk_y - 1) )
//         );
//         w.Start();
//       }
//       else if ( chunkOffset_y >= treshold_upper &&
//                 chunkOffset_x >= treshold_upper ) {
//         Thread t = new Thread(
//           () => PrefetchGpm_nodl( SS.Min(TerrainInit.NUMBER_OF_CHUNKS, chunk_x + 1), SS.Min(TerrainInit.NUMBER_OF_CHUNKS, chunk_y - 1) )
//         );
//         t.Start();
//         Thread u = new Thread(
//           () => PrefetchGpm_nodl( SS.Min(TerrainInit.NUMBER_OF_CHUNKS, chunk_x + 1), chunk_y )
//         );
//         u.Start();
//         Thread w = new Thread(
//           () => PrefetchGpm_nodl( chunk_x, SS.Min(TerrainInit.NUMBER_OF_CHUNKS, chunk_y - 1) )
//         );
//         w.Start();
//       }
      
      // now let's check for edges
      /*else*/ if( chunkOffset_x <= TerrainSettings.GPM_PREFETCH_TRESHOLD && chunk_x >= 1){
        PrefetchGpm_nodl( chunk_x - 1, chunk_y );
      }
      else if( chunkOffset_x >= treshold_upper && chunk_x < TerrainInit.NUMBER_OF_CHUNKS - 1){
        PrefetchGpm_nodl( chunk_x + 1, chunk_y );
      }
      /*else*/ if( chunkOffset_y <= TerrainSettings.GPM_PREFETCH_TRESHOLD && chunk_y >= 1){
        PrefetchGpm_nodl( chunk_x, chunk_y - 1 );
      }
      else if( chunkOffset_y >= treshold_upper && chunk_y < TerrainInit.NUMBER_OF_CHUNKS - 1){
        PrefetchGpm_nodl( chunk_x, chunk_y + 1 );
      }
      
      // if the above didn't trip, we were just kidding with 'edgeTriggered = true'
      else{
        edgeTriggered = false;
      }
      
      if(edgeTriggered){
          print("<< >> << >> edge triggered!");
          lastTriggered = Stopwatch.StartNew();
      }
    }
    else if(edgeTriggered){
      // if edge is triggered, we need to see if we can untrigger it
      if(chunkChange.ElapsedMilliseconds > EDGE_TRIGGER_RESET_DELAY)
        edgeTriggered = false;
      else if( chunkOffset_x <= TerrainSettings.GPM_PREFETCH_TRESHOLD ||
               chunkOffset_x >= treshold_upper                        ||
               chunkOffset_y <= TerrainSettings.GPM_PREFETCH_TRESHOLD ||
               chunkOffset_y >= treshold_upper                        ){
        edgeTriggered = false;
      }
      
      if(!edgeTriggered)
        print("<< << >> >> edge untriggered!");
    }
    
    if(lastChunk_x != chunk_x || lastChunk_y != chunk_y){
      // change of chunk! 
      
      chunkChange = Stopwatch.StartNew();
      
      framesSinceChunkChange = 0;
      
      lastChunk_x = chunk_x;
      lastChunk_y = chunk_y;
      
      chunkTag = (chunk_x * TerrainInit.CHUNK_SIZE) + chunk_x;
        
//       print("entered new chunk! [" + chunk_x + "," + chunk_y + "] — songs: " + GlobalData.chunks[chunk_x,chunk_y].allSongs.Count);
      
      // get background thread to fetch the first items of neighbour chunks (all 8 of them)
      int minChunk_x = max(chunk_x-1, 0);
      int minChunk_y = max(chunk_y-1, 0);
      int maxChunk_x = min(chunk_x+1, TerrainInit.NUMBER_OF_CHUNKS - 1);
      int maxChunk_y = min(chunk_y+1, TerrainInit.NUMBER_OF_CHUNKS - 1);
      
      
      StartCoroutine(PlayChunkTrackWhenReady(GlobalData.chunks[chunk_x, chunk_y]));
      
//       Thread t = new Thread( 
//         () => PrefetchChunkInitialTracks(new AreaBounds(minChunk_x, maxChunk_x, minChunk_y, maxChunk_y), chunk_x, chunk_y)
//       );
//       t.Start();
      
    }
    
    // Unity doesn't like streams and files that grow in size. If you add a song that has been downloaded only partially,
    // unity will only play the bit of the song that was downloaded at that point. This means we need to check whether 
    // we need to reimport the audio clip. We only do this check once every few frames.
    // We also have a check that checks if the song was changed between the checks
    if( (++_update_ccpFrameCounter % _update_clipCheckPeriod) == 0 && musicPlayer.isPlaying){
      StartCoroutine(ClipReloader(_update_currentlyPlaying));
    }
    
    
//     if(! musicPlayer.isPlaying ){
//       if( musicQueue.IsEmpty() )
//         return;
//       StartCoroutine(LoadClip(Application.dataPath + "/Resources/cache/clips/" + musicQueue.Dequeue() + ".wav"));
//     }
  }
}

class AreaBounds{
  public int xmin, xmax, ymin, ymax;
  
  public AreaBounds(int xmin, int xmax, int ymin, int ymax){
    this.xmin = xmin;
    this.xmax = xmax;
    this.ymin = ymin;
    this.ymax = ymax;
  }
}
