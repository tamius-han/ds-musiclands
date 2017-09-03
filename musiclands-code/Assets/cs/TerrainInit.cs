using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using Scaling;
using System.Diagnostics;

[RequireComponent(typeof(Terrain))]
[RequireComponent(typeof(GameObject))]
[RequireComponent(typeof(GameObject))]
[RequireComponent(typeof(GameObject))]
[RequireComponent(typeof(GameObject))]

public class TerrainInit : MonoBehaviour {

  public static float TERRAIN_SIZE_MAX    = 768.0f;     // največji obseg, na katerem dovolimo točke
  public static float TERRAIN_SIZE_MINI   = 256.0f;     // največji obseh, na katerem dovolimo točke pri N < MINI_TERRAIN_LIMIT
  public static int   TERRAIN_SIZE_ACTUAL = 1024;       // dejanska velikost terena, mora biti vselej int && >= TERRAIN_SIZE_MAX
  public static float TERRAIN_MAX_HEIGHT  = 69.0f;      // the tallest peak will be this high
  public static float TERRAIN_MAX_HEIGHT_CLIP = 0.0025f;// the "tallest peak" will have this many points lying higher of it
  public static float TERRAIN_RESOLUTION  = 256.0f;     //
  public static int   CHUNK_LEVEL         = 5;          // on which level of terrainTree will we gather data for our chunks 
  public static int   CHUNK_SIZE          = 1 << CHUNK_LEVEL;
  public static int   NUMBER_OF_CHUNKS    = TERRAIN_SIZE_ACTUAL >> CHUNK_LEVEL;
  public static int   MINIMAL_NUMBER_OF_SONGS_FOR_TERRAIN = 500; // if we're trying to make a map with fewer than this 
                                                                 // amount of songs, we just represent songs with cubes
  public static int   MINI_TERRAIN_LIMIT  = 6000;       // if we have less songs than this, our island will be smaller
  
  public static float PLAYER_INITIAL_X    = 518.0f;     // initial positions for player
  public static float PLAYER_INITIAL_Y    = 518.0f;
  
  public static int   SONG_GROUPING_CHUNK_LEVEL = 4;    // same as chunk level for easy playlist fetching
  
  public static int   LAYERS_MAP          = 8;          // maybe move to some layer enum?
  
  Terrain terrain;
  public TerrainTree terrainTree;
  
  public GameObject menuCam;
  public GameObject player;
  public GameObject peakIndicator;
  public GameObject peakIndicator_fpcam;
  public GameObject peakMarker;
  public GameObject songMarker;
  public GameObject songMarkerMapIndicator;
  
  // misc stuff
  public GameObject cube;
  public GameObject cube2;
  public GameObject cube3;
  public GameObject cube4;
  
  
  float[,] heightmap_raw; //values in this array aren't normalized, but they are interpolated
  List<Peak> peaks;
  
  
  string FIRST_TIME_SETUP_FILE; // Application.dataPath + FIRST_TIME_SETUP_FILE_RELATIVE, initiated in Start()
  string FIRST_TIME_SETUP_FILE_RELATIVE = "/Resources/store/firstTimeSetup";
  
  private GameObject mainMenu;
  
  float terrainStep = 1.0f/1024.0f;  //max height of terrain
  
  string loadScreenProgressMessage = "";
  Text progresstxt;
  GameObject loadingScreen;
  
  void SetLoadingScreenText(string mainMsg){
    progresstxt.text = "<size=\"32\">" + mainMsg + "</size>";
  }
  
  void SetLoadingScreenText(string mainMsg, string subMsg){
    progresstxt.text = "<size=\"32\">"+mainMsg+"</size>\n<size=\"16\">"+subMsg+"</size>";
  }
  
  public void FirstTimeSetup(){
        
    Stopwatch sw = Stopwatch.StartNew();
    
    List<MusicPoint> musicPoints = FileLoader.ReadMusicPoints("/home/tamius-han/Documents/Diplomska 2017/1M_all");

    sw.Stop();
    print("[TerrainInit::FirstTimeSetup] — reading points complete in " + sw.ElapsedMilliseconds + " ms");
    sw = Stopwatch.StartNew();
    
    FileLoader.WriteMusicPointsBin(musicPoints, Application.dataPath + "/Resources/store/datapoints");
    
    sw.Stop();
    print("[TerrainInit::FirstTimeSetup] — writing points to binary file complete in " + sw.ElapsedMilliseconds + " ms");
    
    // create file so we'll know we already went through this step later on
    using (new FileStream(FIRST_TIME_SETUP_FILE, FileMode.Create)) {}
  }
  
  public void InitializeTerrain(){
//     GameObject mainMenu = GameObject.Find("MainMenuCanvas");
    
    mainMenu.SetActive(false);
    loadingScreen.SetActive(true);
    
    

    if(! File.Exists(FIRST_TIME_SETUP_FILE)){
      SetLoadingScreenText("Performing first time setup tasks");
      FirstTimeSetup();
    }
    
    
    StartCoroutine(InitializeTerrainThreadFirstTime());
    
  }

  public void Descend(List<MusicPoint> musicPoints){
    GlobalData.playerHasControl = false;
    loadingScreen.SetActive(true);
    
    StartCoroutine(InitializeTerrainThread(musicPoints, true));
  }
  
  public void Ascend(){
    GlobalData.playerHasControl = false;
    loadingScreen.SetActive(true);
    
    StartCoroutine(InitializeTerrainThread(null, false));
  }
  
  IEnumerator InitializeTerrainThreadFirstTime(){
    GpmConf.Login();
    
    player.SetActive(false);
    menuCam.SetActive(true);
    
    // let's start fresh. 
    CacheOptions.Init();
    
    Stopwatch sw = Stopwatch.StartNew();
    Stopwatch sw1 = Stopwatch.StartNew();
    
    SetLoadingScreenText("Loading data");
    yield return null;                          // We do this so Unity can go and update the text on the loading screen
    
    List<MusicPoint> musicPoints = FileLoader.ReadMusicPointsBin(Application.dataPath + "/Resources/store/datapoints");
    
    sw1.Stop();
    Stopwatch sw2 = Stopwatch.StartNew();
    
    
    print("td data?" + terrain.terrainData.bounds + " .. " + terrain.terrainData.heightmapWidth);
    SetLoadingScreenText("Loading track metadata");
    yield return null;
    
    FileLoader.AddMusicMetadata(musicPoints, "/home/tamius-han/Documents/Diplomska 2017/metadata_1M");
    
    sw2.Stop();
    sw.Stop();
    print("loaded data and metadata in " + sw.ElapsedMilliseconds + " ms | points: " + sw1.ElapsedMilliseconds + " ms; metadata: "+sw2.ElapsedMilliseconds + " ms");
    
//     GlobalData.musicPointStack = new List<List<MusicPoint>>();
//     GlobalData.musicPointStack.Add(musicPoints);
    
    
    StartCoroutine(InitializeTerrainThread(musicPoints, true));
  }
  
  IEnumerator InitializeTerrainThread(List<MusicPoint> musicPoints, bool descend){
    //descend — if true, we go to lower levels. 
    //if false, we restore a previous level from GlobalData
    
    if(GlobalData.peakMarkers != null){
      foreach(GameObject gg in GlobalData.peakMarkers)
        Destroy(gg);
    }
    GlobalData.peakMarkers = new List<GameObject>();
    
    Stopwatch sw = Stopwatch.StartNew();
    Stopwatch sw1 = Stopwatch.StartNew();
    
    if(descend){
      print("--------------------------------------------------");
      print("<InitializeTerrainThread>");
      print("Initializing terrain from " + musicPoints.Count + " datapoints");
      // build terrainTree
      SetLoadingScreenText("Creating terrain tree");
      yield return null;
      
      sw = Stopwatch.StartNew();
      
      terrainTree = new TerrainTree(10);
      float[] mpbounds = GetMusicPointBounds(musicPoints);
      float x_max, y_max, x_min, y_min;
      x_max = mpbounds[0];
      x_min = mpbounds[1];
      y_max = mpbounds[2];
      y_min = mpbounds[3];
      
      int x; int y;
      
      // calculate the difference between northmost/southmost and eastmost/westmost points
      float range_x = x_max - x_min;
      float range_y = y_max - y_min;
      
      // za računanje scaling faktorja bomo uporabili večjo razdalijo.
      float range = range_x > range_y ? range_x : range_y;
      float scaleFactor;
      // največja dovoljena širina je različna v različnih primerih
      if(musicPoints.Count < MINIMAL_NUMBER_OF_SONGS_FOR_TERRAIN)
        scaleFactor = (float)TERRAIN_SIZE_ACTUAL / range;
      else if(musicPoints.Count < MINI_TERRAIN_LIMIT)
        scaleFactor = TERRAIN_SIZE_MINI / range;
      else
        scaleFactor = TERRAIN_SIZE_MAX / range;
      
      // poiščimo sredino med ekstremi
      float midpoint_x = (x_max + x_min)/2;
      float midpoint_y = (y_max + y_min)/2;
      
      float terrain_middle = (float)(TERRAIN_SIZE_ACTUAL >> 1);
      
      int allNodes = musicPoints.Count;
      int currentNode = 0;
      
      
      print("Music points are between x:" + x_min + "-" + x_max + "; y:" + y_min + "-" + y_max + "\nMiddle point is: " + midpoint_x + "," + midpoint_y+"\n\nTerrain middle: " + terrain_middle + "\nRange is this: " + range + "; scale factor is this: " + scaleFactor);
      
      SetLoadingScreenText("Populating terrain tree", (currentNode + "/" + allNodes) );
      yield return null;
      foreach (MusicPoint mp in musicPoints) {
        mp.x = ((mp.x - midpoint_x) * scaleFactor + terrain_middle);
        mp.y = ((mp.y - midpoint_y) * scaleFactor + terrain_middle);
        terrainTree.InsertSong(mp);
        
        if(++currentNode % 5000 == 0){
          SetLoadingScreenText("Populating terrain tree", (currentNode + "/" + allNodes) );
          yield return null;
        }
      }
      
      sw.Stop();
      print("populated tree in " + sw.ElapsedMilliseconds + " ms");
      
      
      SetLoadingScreenText("Calculating averages");
      yield return null;
      
      sw = Stopwatch.StartNew();
      
      terrainTree.CalculateAveragePosition();
      
      sw.Stop();
      print("calculated averages in " + sw.ElapsedMilliseconds + " ms");
      
      
      
      
      
      // time to sort elements by how close of the chunk average they are.
      // We can actually cheat and approximate this because in the end, it doesn't really matter if we get exactly the 
      // "average" song. Processing a great number of songs will take up time otherwise.
      
      // On the flipside, if a chunk has far less songs than square units (a 32x32 chunk will have 1024 spaces inside of it. If a
      // chunk only has 100-something songs, "approximating" will take more time than just processing all the songs.
      
      SetLoadingScreenText("Slicing and dicing the world into chunks");
      yield return null;
      
      sw = Stopwatch.StartNew();
          
      int numOfChunks = TERRAIN_SIZE_ACTUAL / CHUNK_SIZE;
      int numChunksSq = numOfChunks*numOfChunks;
      
      GlobalData.chunks = new TerrainChunk[numOfChunks,numOfChunks];
      
      int globalPos_x, globalPos_y;
      
      for(int i = 0; i < numOfChunks; i++){
        for(int j = 0; j < numOfChunks; j++){
          GlobalData.chunks[i,j] = new TerrainChunk( (i*numOfChunks) + j );
          
          globalPos_x = i * CHUNK_SIZE;
          globalPos_y = j * CHUNK_SIZE;
          
          List<MusicPoint> orderedSongs;
          orderedSongs = TerrainChunk.GetOrderedChunkSongs(terrainTree, globalPos_x, globalPos_y);
          
          GlobalData.chunks[i,j].allSongs = orderedSongs;
          // done
        }
        
        SetLoadingScreenText("Slicing and dicing the world into chunks", ( (i*CHUNK_SIZE) + "/" + numChunksSq) );
        yield return null;
        
      }
      
      sw.Stop();
      print("creating terrain chunks" + sw.ElapsedMilliseconds + " ms");    
      
      if(GlobalData.terrainChunkStack == null)
        GlobalData.terrainChunkStack = new List<TerrainChunk[,]>();
      if(GlobalData.terrainTreeStack == null)
        GlobalData.terrainTreeStack = new List<TerrainTree>();
      
      // adding coordinates to descent history happens in keyctl!
      
      
      GlobalData.terrainChunkStack.Add(GlobalData.chunks);
      GlobalData.terrainTreeStack.Add(terrainTree);
      GlobalData.terrainMagnificationLevel++;
      print("terrainChunkStack size: " + GlobalData.terrainChunkStack.Count + "; magnification level: " + GlobalData.terrainMagnificationLevel);
    }
    else{
      //we're going a level up
      if(GlobalData.terrainMagnificationLevel <= 0){
        yield break; // nope, we're on top level
      }
      SetLoadingScreenText("Restoring old terrain");
      yield return null;
      
      // remove current stuff from the stack in global data
      print("terrainChunk/terrainTreeStack size: " + GlobalData.terrainChunkStack.Count +"/"+ GlobalData.terrainTreeStack.Count + "; magnification level: " + GlobalData.terrainMagnificationLevel);
      
      GlobalData.terrainChunkStack.RemoveAt(GlobalData.terrainMagnificationLevel);
      GlobalData.terrainTreeStack.RemoveAt(GlobalData.terrainMagnificationLevel);
      
      //switch levels:
      GlobalData.terrainMagnificationLevel--;
      
      // restore the level we're rising up to:
      GlobalData.chunks = GlobalData.terrainChunkStack[GlobalData.terrainMagnificationLevel];
      terrainTree = GlobalData.terrainTreeStack[GlobalData.terrainMagnificationLevel];
      
      // remove music cubes (if they exist)
      RemoveMusicCubes();
    }
    
    
    
    // We can start fetching our music in the background, while terrain heightmap is being generated
    // TODO: use actual players' coordinates
    BasicRadioCtl.Init();
//     Thread songFetching = new Thread(
//       () => BasicRadioCtl.LoadInitialTracks(1, 4, 512f, 512f)
//     );
//     
//     songFetching.Start();
    
//     Thread gpmFetch = new Thread(
//       () => BasicRadioCtl.LoadInitialTracksGpm(1,2, 518, 518));
//     gpmFetch.Start();
    
    if( terrainTree.Count() >= MINIMAL_NUMBER_OF_SONGS_FOR_TERRAIN){
      SetLoadingScreenText("Asking Skadi to pls make those mountains", "(This gonna take long tho)"); 
      yield return null;
      
      sw = Stopwatch.StartNew();
      
      
      // Let's create terrain from the data we've read. This can take a while.
      
  //     TerrainLodLevel l00 = new TerrainLodLevel(0, 1.0f);
      TerrainLodLevel l0, l1, l2, l3;
      
      if(terrainTree.Count() > 15000){
        l0 = new TerrainLodLevel(1, 1.0f);
        l1 = new TerrainLodLevel(3, 1.0f);
        l2 = new TerrainLodLevel(4, 1.0f);
        l3 = new TerrainLodLevel(5, 0.50f);
      }
      else{  // a bit different rules for maps with smaller amounts of points
        l0 = new TerrainLodLevel(2, 0.5f);
        l1 = new TerrainLodLevel(4, 1.0f);
        l2 = new TerrainLodLevel(5, 2.0f);
        l3 = new TerrainLodLevel(6, 0.50f);
      }
      TerrainLodLevel[] levels = { /*l00,*/ l0, l1, l2, l3 };
      
      CreateTerrainHeightmap(terrainTree, levels);
      
      sw.Stop();
      print("terrain built in " + sw.ElapsedMilliseconds + " ms");
      
      
      // at this point, we have peaks. This means we're free to make a texture for our terrain:
      
      SetLoadingScreenText("Generating terrain texture"); 
      yield return null;
      
      sw = Stopwatch.StartNew();
      
      Voronoish.FINISHED = false;
      StartCoroutine(Voronoish.GenerateVoronoi(peaks, terrain, TERRAIN_SIZE_ACTUAL, 16));
      
      while(! Voronoish.FINISHED)
        yield return null;
      
      sw.Stop();
      print("terrain painted in " + sw.ElapsedMilliseconds + " ms");
      // add peak and chunk markers
      
      SetLoadingScreenText("Adding peak and chunk markers"); 
      yield return null;
      
      // add peaks (if they weren't added before)
      if(! GlobalData.bottomLevel){
        foreach(Peak p in peaks){
          GameObject txt = Instantiate(peakIndicator_fpcam,
                                       new Vector3( (float)p.x,
                                                    terrain.SampleHeight( new Vector3( (float)p.x,
                                                                                       0f,
                                                                                       (float)p.y
                                                                                     )
                                                                        ) + 16f,
                                                    (float)p.y
                                       ),
                                       Quaternion.identity
          );
          
          Text peakName = txt.transform.FindChild("Canvas/PeakName").gameObject.GetComponent<Text>();
          
          GameObject pm = Instantiate(peakMarker,
                                      new Vector3( (float)p.x,
                                                    terrain.SampleHeight( new Vector3( (float)p.x,
                                                                                      0f,
                                                                                      (float)p.y
                                                                                    )
                                                    ),
                                                    (float)p.y
                                      ),
                                      Quaternion.identity
          );
          GameObject tree = pm.transform.FindChild("Tree").gameObject;
          tree.GetComponent<Renderer>().material.color = p.boxColor;

          int chunk_x = p.x>>TerrainInit.CHUNK_LEVEL;
          int chunk_y = p.y>>TerrainInit.CHUNK_LEVEL;
          
          List<MusicPoint> peakSongs = GlobalData.chunks[chunk_x,chunk_y].allSongs;
          
          if (peakSongs == null || peakSongs.Count == 0){
            peakName.text = "Funny peak    <size=\"16\">there's something wrong with it</size>";
          }
          else{
            string artist = peakSongs[0].meta.Split(new string[] {" - "}, 2, System.StringSplitOptions.None)[0];
            peakName.text = artist + "'s peak";
          }
          
          GameObject go = Instantiate(peakIndicator,
                      new Vector3( (float)p.x, 
                                  300f,  // todo: unhardcode
                                  (float)p.y
                      ),
                      Quaternion.identity
          );
          go.name = "p" + p.id;
          go.tag = "Map";
          go.layer = LAYERS_MAP;
          
          GameObject ccube = go.transform.FindChild("PeakCube").gameObject;
          
          ccube.GetComponent<Renderer>().material.color = p.boxColor;
          GlobalData.peakMarkers.Add(txt);
          GlobalData.peakMarkers.Add(pm);
          GlobalData.peakMarkers.Add(go);
          
        }
      }

      // Biggest marker will be given to chunks that contain 4x the songs an average chunk does.
      
  //     float maxChunkmarkerSize = ((float)(terrainTree.Count()<<2)) / ((float)(NUMBER_OF_CHUNKS*NUMBER_OF_CHUNKS));
  //     float xzScaleFactor, yScaleFactor;
  //     float relativeChunkSize;
  //     
  //     TerrainChunk chunk;
  //     for(int i = 0; i < NUMBER_OF_CHUNKS; i++){
  //       for(int j = 0; j < NUMBER_OF_CHUNKS; j++){
  //         chunk = GlobalData.chunks[i,j];
  //         if(chunk.allSongs.Count == 0)
  //           continue;
  //         
  //         GameObject go = Instantiate(cube2,
  //                                     new Vector3( (float)chunk.allSongs[0].x,
  //                                                  Terrain.activeTerrain.SampleHeight(new Vector3( (float)chunk.allSongs[0].x,
  //                                                                                                  0.0f,
  //                                                                                                  (float)chunk.allSongs[0].y
  //                                                                                     )),
  //                                                  (float)chunk.allSongs[0].y
  //                                     ),
  //                                     Quaternion.identity
  //         );
  //     
  //         relativeChunkSize = Mathf.Min( (((float)chunk.allSongs.Count) / maxChunkmarkerSize), 1.0f);
  //         xzScaleFactor = 0.25f + ( relativeChunkSize ); 
  //         yScaleFactor = 1.0f + (8.0f*relativeChunkSize);
  //         
  //         go.transform.localScale = new Vector3(xzScaleFactor, yScaleFactor, xzScaleFactor);
  //       }
  //     }
      
    }
    else{
      SetLoadingScreenText("Creating some flat lands for the final level", "(This gonna take long tho)"); 
      yield return null;
      
      FlattenTerrain(0.15f);    
      MakeMusicCubes(terrainTree);
    }
    // wait for song downloads to complete
    
    Stopwatch tease = Stopwatch.StartNew();
    string teaseString = "";
    
//     while(songFetching.IsAlive || gpmFetch.IsAlive){
//       if(tease.ElapsedMilliseconds > 80000)
//         teaseString = "   <size=\"12\"> — Any time now</size>";
//       else if(tease.ElapsedMilliseconds > 70000)
//         teaseString = "   <size=\"12\"> — No, Episle 3 doesn't count.</size>";
//       else if(tease.ElapsedMilliseconds > 60000)
//         teaseString = "   <size=\"12\"> — Was Half Life (2 episode) 3 released yet?</size>";
//       else if(tease.ElapsedMilliseconds > 50000)
//         teaseString = "   <size=\"12\"> — Bet you wish you had a decent internet</size>";
//       else if(tease.ElapsedMilliseconds > 40000)
//         teaseString = "   <size=\"12\"> — messing with my wifi. good. pick one, WotC</size>";
//       else if(tease.ElapsedMilliseconds > 30000)
//         teaseString = "   <size=\"12\"> — Wait can metallic dragons disturb my wifi?</size>";
//       else if(tease.ElapsedMilliseconds > 20000)
//         teaseString = "   <size=\"12\"> — Buckle up, get some coffee. This could be a long ride.</size>";
//       else if(tease.ElapsedMilliseconds > 10000)
//         teaseString = "   <size=\"12\"> — And you thought terrain step took ages</size>";
//       
//       SetLoadingScreenText("Downloading initial songs", (GlobalData.songFetching_completed + "/" + GlobalData.songFetching_total + " downloaded" + teaseString));
//       yield return null;
//     }
    
    // place us in the middle of the map 
    
    
    progresstxt.text = "Placing PC";
    yield return null;
    
    float player_x, player_y, player_height;
    
    if(descend){
      player_x = PLAYER_INITIAL_X;
      player_y = PLAYER_INITIAL_Y;
    }
    else{
      if(GlobalData.descentHistory_x.Count <= 0){
        player_x = PLAYER_INITIAL_X;
        player_y = PLAYER_INITIAL_Y;
      }
      else{
        player_x = GlobalData.descentHistory_x[GlobalData.descentHistory_x.Count - 1];
        player_y = GlobalData.descentHistory_y[GlobalData.descentHistory_y.Count - 1];
        
        GlobalData.descentHistory_x.RemoveAt(GlobalData.descentHistory_x.Count - 1);
        GlobalData.descentHistory_y.RemoveAt(GlobalData.descentHistory_y.Count - 1);
      }
    }
    
    player_height = terrain.SampleHeight(new Vector3(player_x, 0f, player_y)) + 2f;
    player.transform.position = new Vector3(player_x, player_height, player_y);
    
    
    // fake delay
    
//     BasicRadioCtl.PlayChunkTrack(terrainTree);
    
    // fertik
    
    loadScreenProgressMessage = "Done lol";
    
    loadingScreen.SetActive(false);
    menuCam.SetActive(false);
    player.SetActive(true);
    GlobalData.playerHasControl = true;
  }
  
  void FlattenTerrain(float height){
    float[,] heightmap = new float[TERRAIN_SIZE_ACTUAL, TERRAIN_SIZE_ACTUAL];
    
    // todo: proper height
    
    for(int i = 0; i < TERRAIN_SIZE_ACTUAL; i++)
      for(int j = 0; j < TERRAIN_SIZE_ACTUAL; j++)
        heightmap_raw[i,j] = height;
    
      
      
    terrain.terrainData.SetHeights(0,0,heightmap);
  }
  
  void CreateTerrainHeightmap(TerrainTree terrainTree, TerrainLodLevel[] terrainLods){
    
    // create raw heightmap and initialize everything to zero:
    heightmap_raw = new float[TERRAIN_SIZE_ACTUAL, TERRAIN_SIZE_ACTUAL];
    
    float observed_max_height = 0.0f;
    float scaleFactor = 1.0f;
    
    for(int i = 0; i < TERRAIN_SIZE_ACTUAL; i++)
      for(int j = 0; j < TERRAIN_SIZE_ACTUAL; j++)
        heightmap_raw[i,j] = 0.0f;
    
    for(int lod = 0; lod < terrainLods.Length; lod++){
      //First: get the heightmap of the current LOD!
      float[,] tmpTerrain = terrainTree.GenerateHeightMap(terrainLods[lod].depth, false);
      
      // Scaling.ScaleImage wants one-dimensional array? On [0,1]?
      
      // we need to get the max value and normalize everything
      float observedMax = 0.0f;
      
      for(int i = 0; i < tmpTerrain.GetLength(0) ; i++)
        for(int j = 0; j < tmpTerrain.GetLength(1) ; j++)
          if(observedMax < tmpTerrain[i,j])
            observedMax = tmpTerrain[i,j];
      
      // now we calculate scaleFactor. We do normalizations as we fill the new array.
      scaleFactor = 1.0f/observedMax;
      
      Scaling.SColor[] flatten = new Scaling.SColor[tmpTerrain.GetLength(0)*tmpTerrain.GetLength(1)];
      float colorComponent = 0.0f;      
      int currentSColor = 0;
      
      for(int i = 0; i < tmpTerrain.GetLength(0); i++){
        for(int j = 0; j < tmpTerrain.GetLength(1); j++){
          colorComponent = scaleFactor * tmpTerrain[i,j];
          flatten[currentSColor] = new SColor(colorComponent, 0.0f, 0.0f, 1.0f);
          currentSColor++;
        }
      }
      
      
      Scaling.ScaleImage l = new Scaling.ScaleImage(flatten, tmpTerrain.GetLength(0));
      Scaling.SColor[] result = l.ScaleLanczos(TERRAIN_SIZE_ACTUAL, TERRAIN_SIZE_ACTUAL);
      
      // scolor is some custom implementation of mono_scaling, so let's add result back to our 2D array
      
      int resultSize = result.Length;
      currentSColor = 0;
      
      // We'd be multiplying result with our multiplier anyway. Why the f*** not restore the number to the original
      // value at the same time, as well?
      scaleFactor = 1.0f / scaleFactor;
      scaleFactor *= terrainLods[lod].multiplier;
      
      for(int i = 0; i < TERRAIN_SIZE_ACTUAL; i++){
        for(int j = 0; j < TERRAIN_SIZE_ACTUAL; j++){          
          heightmap_raw[i,j] += result[currentSColor].r * scaleFactor;
          currentSColor++;
        }
      }
      
      // if we're on peak discovery lod, we should find peaks
      
      if(lod == 3){ //todo: compare by actual LOD vs PEAK_DISCOVERY_LOD
        peaks = FindPeaks(heightmap_raw);
      }
    }
    
    
    // we need to find a new highest point and make all points to fit on [0,1] interval.
    // because all of the intervals we were adding together were between [0,1], the maximum possible
    // height is the sum of all multipliers.
    
    // we're also clipping anomalies — "observed_max_height" will clip away top 5% 
    int clip = (int)( (float)(TERRAIN_SIZE_ACTUAL * TERRAIN_SIZE_ACTUAL) * TERRAIN_MAX_HEIGHT_CLIP);
    
    PriorityQueueF<float> maxObservedClipping = new PriorityQueueF<float>("max");
    
    for(int i = 0; i < heightmap_raw.GetLength(0) ; i++){
      for(int j = 0; j < heightmap_raw.GetLength(1) ; j++){
        maxObservedClipping.Enqueue(heightmap_raw[i,j],heightmap_raw[i,j]);
        if(observed_max_height < heightmap_raw[i,j])
          observed_max_height = heightmap_raw[i,j];
      }
    }
    
    print("<TerrainInit::CreateTerrainHeightmap> Classical observed max height is " + observed_max_height + " while PQF-blessed max height is " + maxObservedClipping.ElementAt(clip - 1) + ". Number of elements higher of max height is " + clip + " and our queue contains " + maxObservedClipping.Count() + " elements.");
    
    // let's not do clipping on small terrains
    if(terrainTree.Count() > MINI_TERRAIN_LIMIT)
      observed_max_height = maxObservedClipping.ElementAt(clip -1);
    
    scaleFactor = TERRAIN_MAX_HEIGHT / observed_max_height;
    
    // Unity requires that heightmap is normalized / on the interval [0,1]
    scaleFactor /= TERRAIN_RESOLUTION;
    
    // Now we create ourselves a new heightmap
    float[,] heightmap = new float[TERRAIN_SIZE_ACTUAL, TERRAIN_SIZE_ACTUAL];
    for(int i = 0; i < TERRAIN_SIZE_ACTUAL ; i++){
      for(int j = 0; j < TERRAIN_SIZE_ACTUAL ; j++){
        heightmap[j,i] = heightmap_raw[i,j] * scaleFactor; // Terrain looks at array a bit differently than we do, flipping x and y
      }
    }
    
    
    terrain.terrainData.SetHeights(0,0,heightmap);
    // gg ez
    
    
  }
  
  void MakeMusicCubes(TerrainTree tt){
    float[,] songCount = tt.GenerateHeightMap(SONG_GROUPING_CHUNK_LEVEL, false);
    
    string artist, title, andmore;
    List<MusicPoint> allSongs;
    
    if(GlobalData.songObjects == null)
      GlobalData.songObjects = new List<GameObject>();
    
    float boxOffset = ((float)(1 << SONG_GROUPING_CHUNK_LEVEL - 1));
    
    int iShifted, jShifted;
    
    for(int i = 0; i < songCount.GetLength(0); i++){
      for(int j = 0; j < songCount.GetLength(1); j++){
        if(songCount[i,j] > 0){
          
          iShifted = i << SONG_GROUPING_CHUNK_LEVEL;
          jShifted = j << SONG_GROUPING_CHUNK_LEVEL;
          
          allSongs = tt.GetSongsInArea(SONG_GROUPING_CHUNK_LEVEL, iShifted, jShifted);
          
          Vector3 boxPosition = new Vector3((float)(iShifted) + boxOffset, 1.0f, (float)(jShifted) + boxOffset);
          
          Peak p = new Peak(0, iShifted, jShifted);
          GameObject go = Instantiate( songMarker,
                                       boxPosition,
                                       Quaternion.identity);
          go.transform.localScale = new Vector3(8.0f, 8f*songCount[i,j], 8.0f);
          go.GetComponent<Renderer>().material.color = p.boxColor;
          GlobalData.songObjects.Add(go);
          
          boxPosition.y = 33f;
          
          GameObject mapMarker = Instantiate( songMarkerMapIndicator,
                                              boxPosition,
                                              Quaternion.identity);
          
          GameObject ccube = mapMarker.transform.FindChild("PeakCube").gameObject;
          ccube.GetComponent<Renderer>().material.color = p.boxColor;
          
          SongItemData sid = (SongItemData) mapMarker.GetComponent(typeof(SongItemData));
          sid.meta = allSongs[0].meta;
          sid.gpmId = allSongs.Count > 1 ? ("... and " + (allSongs.Count - 1) + " more") : "";
          
          GlobalData.songObjects.Add(mapMarker);
        }
      }      
    }
    
  }
  
  void RemoveMusicCubes(){
    if(GlobalData.songObjects == null || GlobalData.songObjects.Count == 0)
      return; // nothing to remove
    
    foreach(GameObject gg in GlobalData.songObjects){
      Destroy(gg);
    }
    
    GlobalData.songObjects = new List<GameObject>();  // never existed
  }
  
  void MakeDebugCubeTerrain(TerrainTree terrainTree){
    terrainTree.CalculateAverageHeight();
    float[,] terrain1 = terrainTree.GenerateHeightMap(5,true);
    float[,] terrain2 = terrainTree.GenerateHeightMap(3,true);
    float[,] terrain3 = terrainTree.GenerateHeightMap(2,true);
    float[,] terrain4 = terrainTree.GenerateHeightMap(1,true);
    
    int t1w, t2w, t3w, t4w;       //terrain width
    t1w = terrain1.GetLength(0);
    t2w = terrain2.GetLength(0);
    t3w = terrain3.GetLength(0);
    t4w = terrain4.GetLength(0);
    
    float cubeScaleFactor = TERRAIN_SIZE_ACTUAL / t1w;
    float cubeScaleFactorHalf = cubeScaleFactor / 2.0f;
    
    print("cube scale factors:");
    print(cubeScaleFactor);
    for(int i = 0; i < t1w; i++){
      for(int j = 0; j < t1w; j++){
        if(terrain1[i,j] == 0.0f)
          continue;
        GameObject go = Instantiate(cube,
                                    new Vector3( (float)i * cubeScaleFactor + cubeScaleFactorHalf,
                                                 terrain1[i,j],
                                                 (float)j * cubeScaleFactor + cubeScaleFactorHalf),
                                    Quaternion.identity);
        go.transform.localScale = new Vector3(cubeScaleFactor, terrain1[i,j], cubeScaleFactor);
      }
    }
    
    
    cubeScaleFactor = TERRAIN_SIZE_ACTUAL / (float)t2w;
    cubeScaleFactorHalf = cubeScaleFactor / 2.0f;
    print(cubeScaleFactor);
    
    for(int i = 0; i < t2w; i++){
      for(int j = 0; j < t2w; j++){
        if(terrain2[i,j] == 0.0f)
          continue;
        
        GameObject go = Instantiate(cube2,
                                    new Vector3( (float)i * cubeScaleFactor + cubeScaleFactorHalf,
                                                 terrain1[i>>2,j>>2] + terrain2[i,j],
                                                 (float)j * cubeScaleFactor + cubeScaleFactorHalf),
                                    Quaternion.identity);
        go.transform.localScale = new Vector3(cubeScaleFactor, terrain2[i,j], cubeScaleFactor);
      }
    }
    
    
    cubeScaleFactor = TERRAIN_SIZE_ACTUAL / (float)t3w;
    cubeScaleFactorHalf = cubeScaleFactor / 2.0f;
    print(cubeScaleFactor);
    
    for(int i = 0; i < t3w; i++){
      for(int j = 0; j < t3w; j++){
        if(terrain3[i,j] == 0.0f)
          continue;
        GameObject go = Instantiate(cube3,
                                    new Vector3( (float)i * cubeScaleFactor + cubeScaleFactorHalf,
                                                 terrain1[i>>3,j>>3] + terrain3[i,j] + terrain2[i>>1,j>>1],
                                                 (float)j * cubeScaleFactor + cubeScaleFactorHalf),
                                    Quaternion.identity);
        go.transform.localScale = new Vector3(cubeScaleFactor, terrain3[i,j], cubeScaleFactor);
      }
    }
    
    
    cubeScaleFactor = TERRAIN_SIZE_ACTUAL / (float)t4w;
    cubeScaleFactorHalf = cubeScaleFactor / 2.0f;
    print(cubeScaleFactor);
    
    for(int i = 0; i < t4w; i++){
      for(int j = 0; j < t4w; j++){
        if(terrain4[i,j] == 0.0f)
          continue;
        GameObject go = Instantiate(cube4,
                                    new Vector3( (float)i * cubeScaleFactor + cubeScaleFactorHalf,
                                                 terrain1[i>>4,j>>4] + terrain2[i>>2,j>>2] + terrain3[i>>1,j>>1] + terrain4[i,j],
                                                 (float)j * cubeScaleFactor + cubeScaleFactorHalf),
                                    Quaternion.identity);
        go.transform.localScale = new Vector3(cubeScaleFactor, terrain4[i,j], cubeScaleFactor);
      }
    }    
  }
  
  void MakeDebugMusicPoints(List<MusicPoint> mplist){
    int[,] terrain = new int[TERRAIN_SIZE_ACTUAL,TERRAIN_SIZE_ACTUAL];
    for(int i = 0; i < TERRAIN_SIZE_ACTUAL; i++)
      for(int j = 0; j < TERRAIN_SIZE_ACTUAL; j++)
        terrain[i,j] = 0;
    
    int x, y;
    foreach(MusicPoint mp in mplist){
      x = (int)mp.x;
      y = (int)mp.y;
      
      terrain[x,y]++;
      
      GameObject go = Instantiate(cube4,
                                  new Vector3( (float)x,
                                                terrain[x,y],
                                                (float)y ),
                                  Quaternion.identity);
      go.name = mp.meta;
    }
  }
  
  List<Peak> FindPeaks(float[,] map){
    Stopwatch sw = Stopwatch.StartNew();
    
    int FP_RADIUS = 5;
    int FP_ITERATIONS = 3;
    int FP_EXCLUSION_ZONE = 96; // no duplo peaks within this distance. 
    
    List<Peak> peaks = new List<Peak>(); 
    int nextId = 0;
    int exclusionZoneSquared = FP_EXCLUSION_ZONE * FP_EXCLUSION_ZONE; //let's calculate this only once
    int distanceX = 0;
    int distanceY = 0;
    
    bool tooClose;
    
//     float[,] smoothMap = Blur.FastBlur(map, FP_RADIUS, FP_ITERATIONS); 
    float[,] smoothMap = map; // we shouldn't get any different results if we just cut blurring out
    
    PriorityQueueF<Peak> peakQueue = new PriorityQueueF<Peak>();
    
    for(int i = 1; i < smoothMap.GetLength(0) - 1; i++){
      for(int j = 1; j < smoothMap.GetLength(1) - 1; j++){
        if(smoothMap[i,j] > smoothMap[i+1, j] &&
           smoothMap[i,j] > smoothMap[i-1, j] &&
           smoothMap[i,j] > smoothMap[i, j+1] &&
           smoothMap[i,j] > smoothMap[i, j-1]
        ){
          // we found a peak? 
          // Let's add it to our super duper priority queue, so we can filter out unwanted peaks later
          // down the line.
          
          // but we still exclude any peaks that aren't tall enough right from the get-go
          if(smoothMap[i,j] < TerrainSettings.LOWEST_ALLOWED_PEAK_HEIGHT)
            continue;
          
          peakQueue.Enqueue(
                      new Peak(nextId++, i, j),
//                       terrain.SampleHeight(new Vector3(j, 0.0f, i))
                      smoothMap[i,j]
                    );
        }
      }
    }
    
    Peak candidate;
    
    while(! peakQueue.IsEmpty()){
      candidate = peakQueue.Dequeue();
      tooClose = false;
      
      //check if our candidate is too close to another peak
      foreach (Peak p in peaks){
        distanceX = p.x - candidate.x;
        distanceY = p.y - candidate.y;
        
        if( ( (distanceX * distanceX) + (distanceY * distanceY) ) < exclusionZoneSquared ){
          tooClose = true;
          break;
        }
      }
      
      if(! tooClose){
        candidate.BuildRelevantSongs(terrainTree);
        //candidate.GenreFetchFirst();
        peaks.Add(candidate);
        
      }
      
    }
    
    sw.Stop();
    print("Found peaks in " + sw.ElapsedMilliseconds + " ms");
    GlobalData.peaks = peaks;
    return peaks;
  }
  
  float[] GetMusicPointBounds(List<MusicPoint> musicPoints){
    // returns an array containing 4 values: 
    //    [ x_max   x_min  y_max   y_min ]
    float x_max, x_min, y_max, y_min;
    
    x_max = Mathf.NegativeInfinity; // števila so lahko kvečjemu večja ali enaka temu
    y_max = Mathf.NegativeInfinity; // zato je najmanjše možno število dobra začetna vrednost
    
    x_min = Mathf.Infinity; // obratna logika kot prej
    y_min = Mathf.Infinity; 
    
    float x, y;
    
    foreach (MusicPoint mp in musicPoints){
      x=mp.GetX();
      y=mp.GetY();
      
      if ( x > x_max ) 
        x_max = x;
      else if ( x < x_min ) // x je lahko najmanjši, samo če ni bil prej največji
        x_min = x;
      
      if ( y > y_max )
        y_max = y;
      else if ( y < y_min )
        y_min = y;
    }
    
    return new float[] { x_max, x_min, y_max, y_min };
  }
  
  // Use this for initialization
  void Start () {
    terrain = GetComponent<Terrain>();
    progresstxt = GameObject.Find("LoadingProgressText").GetComponent<Text>();
    loadingScreen = GameObject.Find("LoadingScreenCanvas");
    FIRST_TIME_SETUP_FILE = Application.dataPath + FIRST_TIME_SETUP_FILE_RELATIVE;
    
    // Initialize python runscripts:
    print(Application.dataPath);
    
    Process foo = new Process();
    foo.StartInfo.FileName = Application.dataPath + "/Resources/scripts/linux/runpy_init.sh";
    foo.StartInfo.Arguments = "";
    foo.Start();
    
    mainMenu = GameObject.Find("MainMenuCanvas");
    
    GpmConf.InitGpm();
  }
  
  // Update is called once per frame
  void Update () {
    if(loadingScreen.activeSelf)
      progresstxt.text = loadScreenProgressMessage;
  }
  
}


