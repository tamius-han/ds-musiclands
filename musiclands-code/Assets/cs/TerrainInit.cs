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
  public static int   TERRAIN_SIZE_ACTUAL = 1024;       // dejanska velikost terena, mora biti vselej int && >= TERRAIN_SIZE_MAX
  public static float TERRAIN_MAX_HEIGHT  = 69.0f;      // the tallest peak will be this high
  public static float TERRAIN_RESOLUTION  = 1024.0f;    //
  public static int   CHUNK_LEVEL         = 5;          // on which level of terrainTree will we gather data for our chunks 
  public static int   CHUNK_SIZE          = 1 << CHUNK_LEVEL;
  
  Terrain terrain;
  TerrainTree terrainTree;
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
    
    List<MusicPoint> musicPoints = FileLoader.ReadMusicPoints("/home/tamius-han/Documents/Diplomska 2017/points20k_pca_run1_ids");

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
    
    StartCoroutine("InitializeTerrainThread");
  }
  
  IEnumerator InitializeTerrainThread(){
    
    if(! File.Exists(FIRST_TIME_SETUP_FILE)){
      SetLoadingScreenText("Performing first time setup tasks");
      FirstTimeSetup();
    }
    
    // let's start fresh. 
    CacheOptions.Init();
    
    Stopwatch sw = Stopwatch.StartNew();
    Stopwatch sw1 = Stopwatch.StartNew();
    
    SetLoadingScreenText("Loading data");
    yield return null;                          // We do this so Unity can go and update the text on the loading screen
    
    List<MusicPoint> musicPoints = FileLoader.ReadMusicPointsBin(Application.dataPath + "/Resources/store/datapoints");
    
    sw1.Stop();
    Stopwatch sw2 = Stopwatch.StartNew();
    
    
    SetLoadingScreenText("Loading track metadata");
    yield return null;
    
    FileLoader.AddMusicMetadata(musicPoints, "/home/tamius-han/Documents/Diplomska 2017/metadata_20k.txt");
    
    sw2.Stop();
    sw.Stop();
    print("loaded data and metadata in " + sw.ElapsedMilliseconds + " ms | points: " + sw1.ElapsedMilliseconds + " ms; metadata: "+sw2.ElapsedMilliseconds + " ms");
    
    
    
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
    float scaleFactor = TERRAIN_SIZE_MAX / range;
    
    // poiščimo sredino med ekstremi
    float midpoint_x = (x_max + x_min)/2;
    float midpoint_y = (x_max + y_min)/2;
    
    float terrain_middle = TERRAIN_SIZE_ACTUAL / 2.0f;
    
    int allNodes = musicPoints.Count;
    int currentNode = 0;
    
    SetLoadingScreenText("Populating terrain tree", (currentNode + "/" + allNodes) );
    yield return null;
    
    foreach (MusicPoint mp in musicPoints) {
      mp.x = ((mp.x * scaleFactor) - midpoint_x + terrain_middle);
      mp.y = ((mp.y * scaleFactor) - midpoint_y + terrain_middle);  // what? why? how? so many questions. | some meddling in TerrainTree fucked that up
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
        if( terrainTree.CountSongsInArea(CHUNK_LEVEL, globalPos_x, globalPos_y) < (CHUNK_SIZE * CHUNK_SIZE) ){
          orderedSongs = TerrainChunk.GetOrderedChunkSongs(terrainTree, globalPos_x, globalPos_y);
        }
        else{
          orderedSongs = TerrainChunk.GetOrderedChunkSongsApproximate(terrainTree, globalPos_x, globalPos_y);
        }
        
        GlobalData.chunks[i,j].allSongs = orderedSongs;
        // done
      }
      
      SetLoadingScreenText("Slicing and dicing the world into chunks", ( (i*CHUNK_SIZE) + "/" + numChunksSq) );
      yield return null;
      
    }
    
    
    sw.Stop();
    print("creating terrain chunks" + sw.ElapsedMilliseconds + " ms");    
    
    // We can start fetching our music in the background, while terrain heightmap is being generated
    BasicRadioCtl.Init();
    Thread songFetching = new Thread(
      () => BasicRadioCtl.LoadInitialTracks(1, 4, 512f, 512f)
    );
    
    songFetching.Start();
    
    SetLoadingScreenText("Asking Skadi to pls make those mountains", "(This gonna take long tho)"); 
    yield return null;
    
    sw = Stopwatch.StartNew();
    
    
    // Let's create terrain from the data we've read. This can take a while.
    
//     TerrainLodLevel l00 = new TerrainLodLevel(0, 1.0f);
    TerrainLodLevel l0 = new TerrainLodLevel(1, 1.0f);
    TerrainLodLevel l1 = new TerrainLodLevel(3, 1.0f);
    TerrainLodLevel l2 = new TerrainLodLevel(4, 1.0f);
    TerrainLodLevel l3 = new TerrainLodLevel(5, 0.50f);
    
    TerrainLodLevel[] levels = { /*l00,*/ l0, l1, l2, l3 };
    
//     TerrainLodLevel tl = new TerrainLodLevel(0,1.0f);
//     TerrainLodLevel[] levels = {tl};
    
    
    CreateTerrainHeightmap(terrainTree, levels);
    
    sw.Stop();
    print("terrain built in " + sw.ElapsedMilliseconds + " ms");
    
    // add peak portals
    
    SetLoadingScreenText("Adding LOD portals (& initiating cache)"); 
    yield return null;
    
//     CacheOptions.Init();
//     foreach(Peak p in peaks){
//       Instantiate(cube3,
//                   new Vector3( (float)p.y, 
//                                Terrain.activeTerrain.SampleHeight(new Vector3((float)p.y, (float)p.x, (float)p.x)),
//                                (float)p.x
//                   ),
//                   Quaternion.identity
//       );
//     }
      // testing the cache and downloading:
      
      
//       if( (CacheOptions.FindItemStatus(p.id) & ~CacheOptions.STATUS_7DIGITAL & CacheOptions.NOT_CACHED) != 0){
//         CacheOptions.FetchItem(p.id, "7digital");
        
//       }
      
//     }
    
    // wait for song downloads to complete
    
    Stopwatch tease = Stopwatch.StartNew();
    string teaseString = "";
    
    while(songFetching.IsAlive){
      if(tease.ElapsedMilliseconds > 80000)
        teaseString = "   <size=\"12\"> — Any time now</size>";
      else if(tease.ElapsedMilliseconds > 70000)
        teaseString = "   <size=\"12\"> — No, Episle 3 doesn't count.</size>";
      else if(tease.ElapsedMilliseconds > 60000)
        teaseString = "   <size=\"12\"> — Was Half Life (2 episode) 3 released yet?</size>";
      else if(tease.ElapsedMilliseconds > 50000)
        teaseString = "   <size=\"12\"> — Bet you wish you had a decent internet</size>";
      else if(tease.ElapsedMilliseconds > 40000)
        teaseString = "   <size=\"12\"> — messing with my wifi. good. pick one, WotC</size>";
      else if(tease.ElapsedMilliseconds > 30000)
        teaseString = "   <size=\"12\"> — Wait can metallic dragons disturb my wifi?</size>";
      else if(tease.ElapsedMilliseconds > 20000)
        teaseString = "   <size=\"12\"> — Buckle up, get some coffee. This could be a long ride.</size>";
      else if(tease.ElapsedMilliseconds > 10000)
        teaseString = "   <size=\"12\"> — And you thought terrain step took ages</size>";
      
      SetLoadingScreenText("Downloading initial songs", (GlobalData.songFetching_completed + "/" + GlobalData.songFetching_total + " downloaded" + teaseString));
      yield return null;
    }
    
    BasicRadioCtl.playerHasControl = true;
    // place us in the middle of the map 
    
    
    progresstxt.text = "Placing PC";
    yield return null;
    
    float playerInitialPosX = 500.0f;
    float playerInitialPosZ = 500.0f;
    
    float playerInitialPosY = terrain.SampleHeight(new Vector3(playerInitialPosX, playerInitialPosX, playerInitialPosZ)) + 2f;
    
//     Camera.main.transform.position = new Vector3(playerInitialPosX, playerInitialPosY, playerInitialPosZ);
    
    
    
    // fake delay
    
//     BasicRadioCtl.PlayChunkTrack(terrainTree);
    
    // fertik
    
    loadScreenProgressMessage = "Done lol";
    
    loadingScreen.SetActive(false);
//     MakeDebugCubeTerrain(terrainTree);
  }
  
  
  void CreateTerrainHeightmap(TerrainTree terrainTree, TerrainLodLevel[] terrainLods){
    
    // create raw heightmap and initialize everything to zero:
    heightmap_raw = new float[TERRAIN_SIZE_ACTUAL, TERRAIN_SIZE_ACTUAL];
    
    float observed_max_height = 0.0f;
    float scaleFactor = 1.0f;
    
    for(int i = 0; i < TERRAIN_SIZE_ACTUAL; i++)
      for(int j = 0; j < TERRAIN_SIZE_ACTUAL; j++)
        heightmap_raw[i,j] = 0.0f;
    
//     for(int i = 0; i < terrainLods.Length; i++){
//       // retreive heightmap from different levels of detail:
//       float[,] tmpTerrain = terrainTree.GenerateHeightMap(terrainLods[i].depth, false);
//       
//       // use bicubic to resize heightmap to terrain resolution
//       float[,] tmpTerrainNative = BicubicInterpolate(tmpTerrain,
//                                                      TERRAIN_SIZE_ACTUAL,
//                                                      TERRAIN_SIZE_ACTUAL,
//                                                      terrainLods[i].multiplier
//                                                     );
//       
//       // add interpolated terrain to raw heightmap, keep track of max height while we're at it 
//       // so we can use it for scaling later
//       
//       for(int l = 0; l < TERRAIN_SIZE_ACTUAL; l++){
//         for(int j = 0; j < TERRAIN_SIZE_ACTUAL; j++){
//           heightmap_raw[l,j] += tmpTerrainNative[l,j];
//           
//           if(heightmap_raw[l,j] > observed_max_height)  // verjetno se odvečne primerjave na tem mestu bolj splačajo kot
//             observed_max_height = heightmap_raw[l,j];   // pa, če bi jih izvedli v ločeni zanki. (nekej nekej cpu cache)
//         }
//       }
//     }
//     
//     
//     
//     // calculate scaling factor for our hills to reach the desired height:
//     scaleFactor = TERRAIN_MAX_HEIGHT / observed_max_height;
//     
//     // Unity requires that heightmap is normalized / on the interval [0,1]
//     scaleFactor /= TERRAIN_RESOLUTION;
//     
//     // Now we create ourselves a new heightmap
//     float[,] heightmap = new float[TERRAIN_SIZE_ACTUAL, TERRAIN_SIZE_ACTUAL];
//     for(int i = 0; i < TERRAIN_SIZE_ACTUAL ; i++){
//       for(int j = 0; j < TERRAIN_SIZE_ACTUAL ; j++){
//         heightmap[i,j] = heightmap_raw[i,j] * scaleFactor;
//       }
//     }
    
    for(int lod = 0; lod < terrainLods.Length; lod++){
      //First: get the heightmap of the current LOD!
      float[,] tmpTerrain = terrainTree.GenerateHeightMap(terrainLods[lod].depth, false);
      
      // Scaling.ScaleImage wants one-dimensional array? On [0,1]?
      
      // we need to get the max value and normalize everything:
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
      
//       if(lod == 3){ //todo: compare by actual LOD vs PEAK_DISCOVERY_LOD
//         peaks = FindPeaks(heightmap_raw);
//       }
    }
    
    
    // we need to find a new highest point and make all points to fit on [0,1] interval.
    // because all of the intervals we were adding together were between [0,1], the maximum possible
    // height is the sum of all multipliers.
    for(int i = 0; i < heightmap_raw.GetLength(0) ; i++)
      for(int j = 0; j < heightmap_raw.GetLength(1) ; j++)
        if(observed_max_height < heightmap_raw[i,j])
          observed_max_height = heightmap_raw[i,j];
    
    scaleFactor = TERRAIN_MAX_HEIGHT / observed_max_height;
    
    // Unity requires that heightmap is normalized / on the interval [0,1]
    scaleFactor /= TERRAIN_RESOLUTION;
    
    // Now we create ourselves a new heightmap
    float[,] heightmap = new float[TERRAIN_SIZE_ACTUAL, TERRAIN_SIZE_ACTUAL];
    for(int i = 0; i < TERRAIN_SIZE_ACTUAL ; i++){
      for(int j = 0; j < TERRAIN_SIZE_ACTUAL ; j++){
        heightmap[i,j] = heightmap_raw[i,j] * scaleFactor;
      }
    }
    
    
    terrain.terrainData.SetHeights(0,0,heightmap);
    // gg ez
    
    
  }
  
  void MakeDebugCubeTerrain(TerrainTree terrainTree){
    float[,] terrain1 = terrainTree.GenerateHeightMap(5,true);
    float[,] terrain2 = terrainTree.GenerateHeightMap(3,true);
    float[,] terrain3 = terrainTree.GenerateHeightMap(2,true);
    float[,] terrain4 = terrainTree.GenerateHeightMap(1,true);
    
    int t1w, t2w, t3w, t4w;       //terrain width
    t1w = terrain1.GetLength(0);
    t2w = terrain2.GetLength(0);
    t3w = terrain3.GetLength(0);
    t4w = terrain4.GetLength(0);
    
    float cubeScaleFactor = TERRAIN_SIZE_ACTUAL / (float)t1w;
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
  
  List<Peak> FindPeaks(float[,] map){
    Stopwatch sw = Stopwatch.StartNew();
    
    int FP_RADIUS = 5;
    int FP_ITERATIONS = 3;
    int FP_EXCLUSION_ZONE = 66; // no duplo peaks within this distance. 
    
    List<Peak> peaks = new List<Peak>(); 
    int nextId = 0;
    int exclusionZoneSquared = FP_EXCLUSION_ZONE * FP_EXCLUSION_ZONE; //let's calculate this only once
    int distanceX = 0;
    int distanceY = 0;
    
    bool tooClose;
    
    float[,] smoothMap = Blur.FastBlur(map, FP_RADIUS, FP_ITERATIONS); 
    
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
          
          // fun fact: unity's terrain's coordinates disagree with our interpretation of the terrain, 
          // which means we need to switch our coordinates
          peakQueue.Enqueue(
                      new Peak(nextId++, i, j),
                      terrain.SampleHeight(new Vector3(j, 0.0f, i))
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
      
      if(! tooClose)
        peaks.Add(candidate);
      
    }
    
    sw.Stop();
    print("Found peaks in " + sw.ElapsedMilliseconds + " ms");
    
    return peaks;
  }
  
  
  
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
  
  // 
  
  
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
  }
  
  // Update is called once per frame
  void Update () {
    if(loadingScreen.activeSelf)
      progresstxt.text = loadScreenProgressMessage;
  }
  
  // todo: move this to a separate script
  void LateUpdate() {
    Cursor.visible = true;
  }
  
}


