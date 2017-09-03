using System.Collections;
using System.Collections.Generic;

using System.Diagnostics;
using UnityEngine;

public class FFmpeg : MonoBehaviour {
  
  private static string ffmpegExec = "/usr/bin/ffmpeg";
  
  // class that makes sure we run correct python commands on correct platforms
  public static int Convert(string filenameIn, string filenameTarget){
    return Convert(filenameIn, filenameTarget, -1);
  }
  
  public static int Convert(string filenameIn, string filenameTarget, int id){
    
    // things are different on different operating systems, so on the first python script that we run, 
    // we also check what OS we're on and handle things appropriately
    if (ffmpegExec == "") {
      // todo: read executable name from settings
      ffmpegExec = "/usr/bin/ffmpeg";
    }
    
    
    Process proc = new Process();
    proc.StartInfo.FileName = ffmpegExec;
    proc.StartInfo.Arguments = "-i " + filenameIn + " -vn -acodec libvorbis " + filenameTarget + "  -y";
    proc.Start();
    
    if(id != -1){
      print("adding stream to download list!");
      
      DownloadCtl.StopLastDownload(proc, id);
      
    }
    
    proc.WaitForExit();
    return proc.ExitCode;
  }
  
  public static int Stream(string streamUrl, string filenameTarget){
    // scoobydoo.jpg
    // "That's Convert()!"
    // "And I would have gotten away if it weren't for these blasted kids and their dog!"
    //
    // Jokes aside, this wrap actually makes sense from readability perspective.
    
    return Convert(streamUrl, filenameTarget, -1);
  }
  
  public static int Stream(string streamUrl, string filenameTarget, int id){
    return Convert(streamUrl, filenameTarget, id);
  }
  
}
