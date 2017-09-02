using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(GameObject))]

public class BasicRadioCtl : MonoBehaviour {

  public AudioSource musicPlayer;
  
  static TerrainChunk[,] chunks;
  static GameObject player;
  
  static ThreadsafeQueue<int> musicQueue;
  
  static float chunkFraction;
  static bool initialized = false;
  public static bool playerHasControl;
  int lastChunk_x, lastChunk_y, chunkTag;
  
  
  
  
  
  /** non-static stuff **/
  
  Text stationText, songInfoText;
  
  
  
  
  /** **/
  
  public static void Init(){
    
    player = GameObject.Find("Player");
//     musicPlayer = GameObject.FindSceneObjectsOfType(typeof(AudioSource))[0] as AudioSource;
    
    if(musicQueue == null)
      musicQueue = new ThreadsafeQueue<int>();
    
    
    musicQueue.Drop(); // chunk tags won't work on re-initialization because we go one level down
    
    initialized = true;
  }
  
  public static void LoadChunksAroundPC(TerrainTree tt, int range){
    
    float playerX = player.transform.position.x;
    float playerY = player.transform.position.z;
    
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
  
  IEnumerator PlayChunkTrackWhenReady(TerrainChunk chunk){
    
    List<MusicPoint> mplist = chunk.allSongs;
    
    MusicPoint nowPlay = null;
    
    
    foreach(MusicPoint mp in mplist){
//       print("musicpoint with ewewid " + mp.id + " has status " + CacheOptions.FindItemStatus(mp.id));
      if( (CacheOptions.FindItemStatus(mp.id) & (CacheOptions.PLAYABLE | CacheOptions.DOWNLOADING | CacheOptions.DOWNLOADED)) != 0 ){
        nowPlay = mp;
        break;
      }
    }
    
    if(nowPlay == null){  // nothing to do if we haven't found a suitable id
      SetSongInfo("«no songs to be found in these parts»","");
      yield break;
    
    }
    // let's load the song with ID
    
    while( (CacheOptions.FindItemStatus(nowPlay.id) & ( CacheOptions.DOWNLOADED | CacheOptions.PLAYABLE )) == 0 )
      yield return null;
    
    
    
    int status = CacheOptions.FindItemStatus(nowPlay.id);
    
    // if song isn't in a playable state, we run conversion
    if( (status & CacheOptions.PLAYABLE ) == 0 ){
      Thread t = new Thread(
        () => FFmpeg.Convert(GlobalData.cachedir + "/clips/" + nowPlay.id + ".mp3", GlobalData.cachedir + "/clips/" + nowPlay.id + ".wav"));
      t.Start();
      while(t.IsAlive)
        yield return null;
    }
    
    if(nowPlay.artist == null || nowPlay.title == null){
      if(nowPlay.GetMeta() != null){
        SetSongInfo(nowPlay.GetMeta());
      }
      else{
        SetSongInfo("«no metadata for this song»","");
      }
    }
    
    StartCoroutine(PlayClip(nowPlay.id));
  }
  
  
  IEnumerator PlayClip(int id){
    
    // we assume we already converted to ffmpeg.
    
    string path = GlobalData.cachedir + "/clips/" + id + ".wav";
    
    WWW www = new WWW("file://" + path);
//     print("loading " + path);
    
    AudioClip clip = www.GetAudioClip(false);
    while(!clip.isReadyToPlay){
      yield return www;
    }
    
//     print("done loading");
    //     clip.name = Path.GetFileName(path);
    
    musicPlayer.clip = clip;
    musicPlayer.Play();
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
  }
  
  // Update is called once per frame
  void Update () {
    if( ! (initialized && playerHasControl) )
      return;
    
    // which chunk are we in?
    // TODO: which peak is the closest to us
    
    float playerX = player.transform.position.x;
    float playerY = player.transform.position.z;
    
    int chunk_x = ((int)playerX) / TerrainInit.CHUNK_SIZE;
    int chunk_y = ((int)playerY) / TerrainInit.CHUNK_SIZE;
    
    if(lastChunk_x != chunk_x && lastChunk_y != chunk_y){
      // change of chunk! 
      
      lastChunk_x = chunk_x;
      lastChunk_y = chunk_y;
      
      chunkTag = (chunk_x * TerrainInit.CHUNK_SIZE) + chunk_x;
        
      print("entered new chunk!");
      
      StartCoroutine(PlayChunkTrackWhenReady(GlobalData.chunks[chunk_x, chunk_y]));
    }
    
    
    
//     if(! musicPlayer.isPlaying ){
//       if( musicQueue.IsEmpty() )
//         return;
//       StartCoroutine(LoadClip(Application.dataPath + "/Resources/cache/clips/" + musicQueue.Dequeue() + ".wav"));
//     }
  }
}

