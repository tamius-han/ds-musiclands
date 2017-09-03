using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using RestSharp.Contrib;
using UnityEngine;

public class FileLoader : MonoBehaviour{
  
  public static List<MusicPoint> ReadMusicPoints(string fileIn){
    List<MusicPoint> points = new List<MusicPoint>();
    
    System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
    
    print("trying to read file: " + fileIn);
    
    try{
      string line;
      StreamReader sr = new StreamReader(fileIn, Encoding.Default);
      
      int counter = 0;
      
      using(sr){
        line = sr.ReadLine();
        
        
        
        while (line != null ){
          string[] args = line.Split(null);
//           print("line: " + line + "; args[0]: " + args[0] + "; parse float: " + float.Parse(args[0]) );
//           print("Adding new point at "+ args[0] +", " + args[1] );
          if(args.Length == 3)
            points.Add(new MusicPoint(int.Parse(args[0]), float.Parse(args[1]), float.Parse(args[2]) ) );
          else if(args.Length == 2)
            points.Add(new MusicPoint(-1, float.Parse(args[0]), float.Parse(args[1]) ) );
          
          line = sr.ReadLine();
        }
      }
    }
    catch(Exception e){
      Console.WriteLine("Something went wrong. Trace:\n{0}\n", e.Message);
    }
    print("number of points: " + points.Count);
    return points;
    
  }
  
  public static void AddMusicMetadata(List<MusicPoint> musicPoints, string fileIn){
    print("metadata file: " + fileIn);
//     try{
      string line;
      StreamReader sr = new StreamReader(fileIn, Encoding.Default);
      
      int counter = 0;
      
      using(sr){
        line = sr.ReadLine();
        MusicPoint mp = musicPoints[0];
        
        while (line != null){
          // let's get number/id in a nice, special, separate place from the rest of the string
          string[] args = line.Split(new char[]{'\t', ' '}, 2);
          
          if( mp.GetId() == int.Parse(args[0]) ){
            mp.SetMeta(HttpUtility.HtmlDecode(args[1]));              
            counter++;
            if(counter < musicPoints.Count)
              mp = musicPoints[counter];
            else
              break;              
          }
          
          line = sr.ReadLine();
        }
      }
      Debug.Log("[FileLoader::AddMusicMetadata] meta added. Total: " + counter + "/" + musicPoints.Count);
//     }
//     catch(Exception e){
//       Console.WriteLine("Something went wrong. Trace:\n{0}\n", e.Message);
//     }
  }
  
  public static List<MusicPoint> ReadMusicPointsBin(string fileIn){
    List<MusicPoint> points = new List<MusicPoint>();
    
    System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
    
    print("[FileLoader::ReadMusicPointsBin] trying to read file: " + fileIn);
    
    if(File.Exists(fileIn)){
      int id;
      float x, y;
      using (BinaryReader reader = new BinaryReader(File.Open(fileIn, FileMode.Open))){
        while(reader.BaseStream.Position != reader.BaseStream.Length){
          id = reader.ReadInt32();
          x  = reader.ReadSingle();
          y  = reader.ReadSingle();
          
          points.Add(new MusicPoint(id, x, y));
        }
      }
    }
    
    print("number of points: " + points.Count);
    return points;
  }
  
  public static void WriteMusicPointsBin(List<MusicPoint> musicPoints, string fileOut){    
    
    using (BinaryWriter writer = new BinaryWriter(File.Open(fileOut, FileMode.Create))){
      foreach(MusicPoint mp in musicPoints){
        writer.Write(mp.id);
        writer.Write(mp.x);
        writer.Write(mp.y);
      }
    }
  }
  
}
