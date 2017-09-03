using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(GameObject))]

public class BasicRadioCtl : MonoBehaviour {

  public GameObject GameUiPanel;
  public GameObject BottomLevelUi;
  public GameObject showContentMsg;
  public GameObject musicManagerPanel;
  
  public GameObject songList;
  public GameObject songItem;
  
  
  public static int PREFETCH_7DIGITAL = 1;
  public static int PREFETCH_GPM = 1;
  public static string PROVIDER_PREFERENCE = "gpm";
  
  
  
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

    Thread sd  = new Thread( () => { Prefetch7digital(ab, chunk_x, chunk_y); print("7digital prefetched"); } );
    Thread gpm = new Thread( () => { PrefetchGpm(ab, chunk_x, chunk_y); print("gpm prefetched"); } );
    sd.Start();
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
        
        CacheOptions.FetchFirstN( GlobalData.chunks[i, j], PREFETCH_GPM, "gpm");
      }
    }
  }
  static void PrefetchGpm(TerrainChunk chunk){
    
    CacheOptions.FetchFirstN( chunk, PREFETCH_GPM, "gpm");
  }
  
  
  IEnumerator PlayChunkTrackWhenReady(TerrainChunk chunk){
    
    Thread prefetch = new Thread( () => PrefetchGpm(chunk) );
    prefetch.Start();
    
    SetSongInfo("« Looking up songs »","");
    
    while(prefetch.IsAlive)
      yield return null;
    
    List<MusicPoint> mplist = chunk.allSongs;    
    MusicPoint nowPlay = null;
    MusicPoint mp;
    
    int allowedStatuses = /*CacheOptions.PLAYABLE | CacheOptions.DOWNLOADING | CacheOptions.DOWNLOADED | 
                          */CacheOptions.GPM_READY | CacheOptions.GPM_STREAMING | CacheOptions.GPM_CACHED;
    
    int chunkSongIterator = 0;
    
    restartpls:
    if(chunkSongIterator >= mplist.Count)
      yield break;
    
    while(chunkSongIterator < mplist.Count){
      mp = mplist[ (chunk.currentId + chunkSongIterator) % mplist.Count ];   // we can do up to one full circle
      
      if( (CacheOptions.FindItemStatus(mp.id) & allowedStatuses) != 0 ){
        chunk.currentId = (chunk.currentId + chunkSongIterator) % mplist.Count;   // skip unready chunks for the next time
        nowPlay = mp;
        break;
      }
      
      chunkSongIterator++;
    }
    
    if(nowPlay == null){  // nothing to do if we haven't found a suitable id
      if(chunk.allSongs.Count == 0){
        SetSongInfo("«no songs to be found in these parts»","");
        yield break;
      }
      else{
        SetSongInfo("<color=\"#88c\">« songs are currently unavailable »</color>","");
        yield break;  // todo: refresh if chunk hasn't changed or something
      }
    }
    
    // Let's wait until our song becomes available. That shouldn't take long
    allowedStatuses = /*CacheOptions.DOWNLOADED | CacheOptions.PLAYABLE | */  CacheOptions.GPM_READY  | CacheOptions.GPM_STREAMING | CacheOptions.GPM_CACHED;
                      
    SetSongInfo("<color=\"f00\">«Slow down! Fetch can't catch you.»</color>","<color=\"#f61\">«you move too fast on the space, [Fetch()] don't chase you»</color>");
    while( (CacheOptions.FindItemStatus(nowPlay.id) & allowedStatuses) == 0 ){
      yield return null;
    }
    SetSongInfo("« catching up »","");
    int status = CacheOptions.FindItemStatus(nowPlay.id);
    
    // we came this far, song should be either downloaded 7digital or ready to be streamed from gpm (at the very worst)
    
    // If song is available from 7digital, we play the 7digital one by default (7digital is guaranteed to play the exact
    // song it's supposed to, GPM does best guess based on metadata we have (artist - title —> not much))
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
//     
//     if( (status & (CacheOptions.GPM_CACHED | CacheOptions.GPM_STREAMING)) != 0 ){
//       // available from our GMP cache!
//       StartCoroutine(PlayFull(nowPlay));
//       yield break;
//     }
    
    int yieldCounter = 0;
    
    if( (status & CacheOptions.GPM_READY ) != 0){
      // start streaming from GPM
      
      print("gpm id of the song is {" + nowPlay.gpmId + "} — getting stream URL");
      
      string streamUrl = "";
      Thread gsu = new Thread( () => streamUrl = GpmConf.GetStream(nowPlay.gpmId) );
      gsu.Start();
      
      while(gsu.IsAlive)
        yield return null;
      
      
      if(streamUrl == ""){
        print("no url, using a dummy stream instead");
        streamUrl = Application.dataPath + "/Resources/dummySound/3-The_Ziggurat.flac";
      }
      print("got stream URL: " + streamUrl);
      
      Thread t = new Thread( () => CacheOptions.StartGpmStream(nowPlay.id, streamUrl) );
      t.Start();

      Stopwatch timeout = Stopwatch.StartNew();

      while(! File.Exists(GlobalData.cachedir + "/full/" + nowPlay.id + ".ogg") ){ // wait until ffmpeg starts downloading
        if(timeout.ElapsedMilliseconds <= 2000) // allow a short while for the file to appear. 2000msec.
          yield return null;
        else{
          print("does file ["+ GlobalData.cachedir + "/full/" + nowPlay.id + ".ogg" +"] exist? " + File.Exists(GlobalData.cachedir + "/full/" + nowPlay.id + ".ogg"));
//           goto restartpls;
          yield break;
        }
      }
      System.Threading.Thread.Sleep(500); // 500ms should be enough buffer
      
      StartCoroutine(PlayFull(nowPlay));
    }
    
    if ( (status & (CacheOptions.GPM_STREAMING | CacheOptions.GPM_CACHED) ) != 0 ){
      // we're already streaming
      StartCoroutine(PlayFull(nowPlay));
    }
    
    /*    
    if(PROVIDER_PREFERENCE != "7digital"){
      if( (status & CacheOptions.PLAYABLE) != 0){
        // good, is playable. Let's play the clip thxbai
        StartCoroutine(PlayClip(nowPlay));
        yield break;
      }
      // must be non-playable, then.
      Thread t = new Thread(
        () => FFmpeg.Convert(GlobalData.cachedir + "/clips/" + nowPlay.id + ".mp3", GlobalData.cachedir + "/clips/" + nowPlay.id + ".wav"));
      t.Start();
      
      while(t.IsAlive)
        yield return null;
      
      StartCoroutine(PlayClip(nowPlay));
    }*/
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
    int waitForLoadstate = 200;
    int loadStateAttempts = 3;
    int i;
    
    string path = GlobalData.cachedir + "/full/" + mp.id + ".ogg";
    WWW www;
    AudioClip clip;
    
    retry:
    
    i = waitForLoadstate;
    www = new WWW("file://" + path);
    clip = www.GetAudioClip(false, true);
    
    while(clip.loadState == AudioDataLoadState.Unloaded){
      // the null lance: "once zero kills i"
      if(i --<= 0){
        if( loadStateAttempts --> 0){
          goto retry;
        }
        else{
          SetSongInfo(mp.meta, "<color=\"#f00\">Unable to get satisfactory loadstate. Quitting for this song</color>");
          yield return null;
          yield break;
        }
      }
      SetSongInfo(mp.meta, "Clip present but not ready to play");
      
      yield return www;
    }
    SetSongInfo("<color=\"#f60\">[gpm] </color>" +mp.meta);
    
    clip.name = "gpm." + mp.id;
    musicPlayer.clip = clip;
    musicPlayer.Play();
    musicPlayer.timeSamples = 0; // force to start at the beginning, our ClipReloader will mess up start positions
    _update_currentlyPlaying = mp;
  }
  
  IEnumerator ClipReloader(MusicPoint mp){
    
    int status = CacheOptions.FindItemStatus(mp.id);
    
//     if( musicPlayer.clip.loadState == AudioDataLoadState.Unloaded ) // we won't even try if that's the case
//       yield break;
//     
    
    // that leaves us with GPM clips that play
    string path = GlobalData.cachedir + "/full/" + mp.id + ".ogg";
    WWW www = new WWW("file://" + path);
    
    AudioClip newClip = www.GetAudioClip(false);

    Stopwatch sw = Stopwatch.StartNew();
    
    while(newClip.loadState == AudioDataLoadState.Unloaded){
      if(sw.ElapsedMilliseconds > 1500){
        print("[BasicRadioCtl::ClipReloader()] whoopsie the loadstate didn't get out of unloaded for too long. not even trying");
        sw.Stop();
        AudioClip.Destroy(newClip, 0f);
        www.Dispose();
        yield break;
      }
      yield return www;
    }
    sw.Stop();
    newClip.name = "gpm." + mp.id;
    
    // name must also match. If the names don't match, the clip was changed during the coroutine, so we don't do it.
    if ( musicPlayer.clip.length != newClip.length && musicPlayer.clip.name == newClip.name ) {
      print("[BasicRadioCtl::ClipReloader()] Reloading the clip!      ");
      
      
      
      int currentTime = musicPlayer.timeSamples;
      AudioClip.Destroy(musicPlayer.clip, 0f);        // memory leak fixed?
      musicPlayer.clip = newClip;
      musicPlayer.Play();
      
//       if(musicPlayer.timeSamples > currentTime){
        musicPlayer.timeSamples = currentTime;
//       }
    }
    else{
      audioClipCorrected = true;             // set this variable to true, so clipReloader won't run needlessly
      AudioClip.Destroy(newClip);            // memory leak fixed some more
    }
    
    www.Dispose();                           // extra memory leak fixes
  }
  
  public IEnumerator PlaySong(MusicPoint mp, GameObject listItem){
    // plays song from bottom level popups
    
    Text statusLabel = (Text) listItem.transform.FindChild("Item/Status").gameObject.GetComponent<Text>();
    Text artist      = (Text) listItem.transform.FindChild("Item/Artist").gameObject.GetComponent<Text>();
    Text title       = (Text) listItem.transform.FindChild("Item/Title").gameObject.GetComponent<Text>();
    
    Button playButton  = (Button) listItem.transform.FindChild("Item/PlayButton").gameObject.GetComponent<Button>();
    Button queueButton = (Button) listItem.transform.FindChild("Item/QueueButton").gameObject.GetComponent<Button>();
    
    if(mp.gpmId == ""){
      statusLabel.text = "<color=\"#aa6\">?</color>";
      
      Thread find = new Thread( () => mp.gpmId = CacheOptions.FindGpm(mp) );
      find.Start();
      
      while(find.IsAlive)
        yield return null;
    }
    
    if(mp.gpmId == "GPM_ERROR" || mp.gpmId == "NO_HITS\n" || mp.gpmId == "NO_STORE_HITS\n"){
      statusLabel.text = "<color=\"#F30\"><size=16>[err]</size></color>";
      playButton.interactable = false;
      playButton.interactable = false;
      yield break;
    }
    
    statusLabel.text = "<color=\"#FF2\"><size=16>[...]</size></color>";
    
    string streamUrl = "";
    Thread gsu = new Thread( () => streamUrl = GpmConf.GetStream(mp.gpmId) );
    gsu.Start();
    while(gsu.IsAlive)
      yield return null;
        
    if(streamUrl == ""){
      statusLabel.text = "<color=\"#F30\"><size=16>[err]</size></color>";
      playButton.interactable = false;
      playButton.interactable = false;
      yield break;
    }
    
    Thread t = new Thread( () => CacheOptions.StartGpmStream(mp.id, streamUrl) );
    t.Start();
    
    Stopwatch timeout = Stopwatch.StartNew();
    
    while(! File.Exists(GlobalData.cachedir + "/full/" + mp.id + ".ogg") ){ // wait until ffmpeg starts downloading
      if(timeout.ElapsedMilliseconds <= 5000) // wait up to 5 seconds for file to appear
        yield return null;
      else{                                   // raise error
        statusLabel.text = "<color=\"#F30\"><size=16>[err]</size></color>";
//         playButton.interactable = false;
//         playButton.interactable = false;
        yield break;
      }
    }
    System.Threading.Thread.Sleep(500); // 500ms should be enough buffer
    
    StartCoroutine(PlayFull(mp));
    
    statusLabel.text = "<color=\"#4f6\"><size=16>[ok]</size></color>";
    yield return null;
  }

  
  void ToggleBottomLevelUI(bool bottomLevel){
    GameUiPanel.SetActive(! bottomLevel);
    BottomLevelUi.SetActive(bottomLevel);
  }
  
  void StopAudioBottomLevel(bool bottomLevel){
    // todo: fadeout when reaching the bottom level
    musicPlayer.Stop();
    
    // drop the user-generated music queue
    bottomLevelMusicQueue = new List<MusicPoint>();
  }
  
  public void ShowSongList(TerrainTree tt){
    showContentMsg.SetActive(false);
    
    // clear old list:
    foreach(Transform child in songList.transform.FindChild("Viewport/Content")){
      GameObject.Destroy(child.gameObject);
    }
    
    float player_x = player.transform.position.x;
    float player_y = player.transform.position.z;
    
    List<MusicPoint> allSongs = tt.GetSongsInArea(TerrainInit.SONG_GROUPING_CHUNK_LEVEL, player_x, player_y);
    
    SongItemData sid;
    
    print("showing song list. we have this many songs: " + allSongs.Count);
    
    for(int i = 0; i < allSongs.Count; i++){
      GameObject item = Instantiate(songItem);
      sid = (SongItemData) item.GetComponent(typeof(SongItemData));
      
      sid.id = allSongs[i].id;
      sid.meta = allSongs[i].meta;
      sid.gpmId = allSongs[i].gpmId;
      
      item.transform.position = new Vector3(0f, (float)(i * -50), 0f);
      
      // set title and everything
      
      int status = CacheOptions.FindItemStatus(sid.id);
      Text statusLabel = (Text) item.transform.FindChild("Item/Status").gameObject.GetComponent<Text>();
      Text artist      = (Text) item.transform.FindChild("Item/Artist").gameObject.GetComponent<Text>();
      Text title       = (Text) item.transform.FindChild("Item/Title").gameObject.GetComponent<Text>();
      
      Button playButton  = (Button) item.transform.FindChild("Item/PlayButton").gameObject.GetComponent<Button>();
      Button queueButton = (Button) item.transform.FindChild("Item/QueueButton").gameObject.GetComponent<Button>();
      
      string stat;
      
      // status symbol processing
      if( (status & (CacheOptions.GPM_CACHED | CacheOptions.GPM_READY | CacheOptions.GPM_STREAMING | CacheOptions.DOWNLOADED | CacheOptions.PLAYABLE)) != 0){
        stat = "<color=\"#3F6\"><size=16>[ok]</size></color>";
      }
      else if ( (status & (CacheOptions.GPM_NOT_AVAILABLE | CacheOptions.NOT_AVAILABLE) ) == (CacheOptions.GPM_NOT_AVAILABLE | CacheOptions.NOT_AVAILABLE)){
        stat = "<color=\"#F30\"><size=16>[err]</size></color>";
        playButton.interactable = false;
        queueButton.interactable = false;
      }
      else{
        stat = "?";
      }
      
      // add artist
      string[] metasplit = SS.SplitMeta(sid.meta);
      
      statusLabel.text = stat;
      artist.text = metasplit[0];
      title.text = metasplit[1];
      
      item.transform.SetParent(songList.transform.FindChild("Viewport/Content"), false);
    }
    
    musicManagerPanel.SetActive(true);
    Cursor.visible = true;
    Screen.lockCursor = false;
  }
  
  // we do updates on bottom level differently
  void BottomLevelUpdate(){
    
  }
  
  
  // global variables for function update
  
  bool bottomLevelHistory = false;  // if GlobalData.bottomLevel != bottmLevelHistory -> toggle interface
  
  MusicPoint _update_currentlyPlaying = null;
  
  int _update_clipCheckPeriod = 2000; // msec
  Stopwatch _update_clipCheckTimer;
  
  List<MusicPoint> bottomLevelMusicQueue;
  
  bool audioClipCorrected = false;
  
  

  void Update () {    
    if( ! (initialized && GlobalData.playerHasControl) )
      return;
    
    // move audiosource to player, always
    gameObject.transform.position = player.transform.position;
    
    
    // toggle appropriate UIs
    if( GlobalData.bottomLevel != bottomLevelHistory ){
      bottomLevelHistory = GlobalData.bottomLevel;
      ToggleBottomLevelUI(GlobalData.bottomLevel);
      StopAudioBottomLevel(GlobalData.bottomLevel);
    }
    
    
    // Unity doesn't like streams and files that grow in size. If you add a song that has been downloaded only partially,
    // unity will only play the bit of the song that was downloaded at that point. This means we need to check whether 
    // we need to reimport the audio clip. We only do this check once every few frames.
    // We also have a check that checks if the song was changed between the checks
    // 
    // This check runs on all levels
    if(musicPlayer.isPlaying && _update_clipCheckTimer.ElapsedMilliseconds > _update_clipCheckPeriod ){
      StartCoroutine(ClipReloader(_update_currentlyPlaying));
      _update_clipCheckTimer.Reset();
      _update_clipCheckTimer.Start();
    }
    
    
    
    
    if( GlobalData.bottomLevel ){
      BottomLevelUpdate();
      return;
    }
      
//     if( ! player.activeSelf ){
//       musicPlayer.Stop();
//       return;
//     }
    
    // which chunk are we in?
    // TODO: which peak is the closest to us
    
    float playerX = player.transform.position.x;
    float playerY = player.transform.position.z;
    
    int chunk_x = ((int)playerX) >> TerrainInit.CHUNK_LEVEL;
    int chunk_y = ((int)playerY) >> TerrainInit.CHUNK_LEVEL;
    
    
    if(Input.GetKeyDown(KeyCode.C)){
      Voronoish.GenerateVoronoiDebug((int)playerX, (int)playerY, GlobalData.peaks);
    }
    
    
    if(lastChunk_x != chunk_x || lastChunk_y != chunk_y){
      // change of chunk! 
      
      lastChunk_x = chunk_x;
      lastChunk_y = chunk_y;
      
      audioClipCorrected = false;
      
      chunkTag = (chunk_x * TerrainInit.CHUNK_SIZE) + chunk_x;
        
//       print("entered new chunk! [" + chunk_x + "," + chunk_y + "] — songs: " + GlobalData.chunks[chunk_x,chunk_y].allSongs.Count);
      
      // get background thread to fetch the first items of neighbour chunks (all 8 of them)
      int minChunk_x = max(chunk_x-1, 0);
      int minChunk_y = max(chunk_y-1, 0);
      int maxChunk_x = min(chunk_x+1, TerrainInit.NUMBER_OF_CHUNKS - 1);
      int maxChunk_y = min(chunk_y+1, TerrainInit.NUMBER_OF_CHUNKS - 1);
      
      
      StartCoroutine(PlayChunkTrackWhenReady(GlobalData.chunks[chunk_x, chunk_y]));
       
    }
    
    
    
  }
  
  void Start() {
    if(musicQueue == null)
      musicQueue = new ThreadsafeQueue<int>();
    
    int numOfChunks = TerrainInit.TERRAIN_SIZE_ACTUAL / TerrainInit.CHUNK_SIZE;
    chunks = new TerrainChunk[numOfChunks,numOfChunks];
    musicPlayer = GameObject.FindSceneObjectsOfType(typeof(AudioSource))[0] as AudioSource;
    
    lastChunk_x = 0;
    lastChunk_y = 0;
    
//     stationText = GameObject.Find("StationInfo").GetComponent<Text>();
    songInfoText = GameObject.Find("SongInfo").GetComponent<Text>();
    
    bottomLevelMusicQueue = new List<MusicPoint>();
    
    _update_clipCheckTimer = Stopwatch.StartNew();
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
