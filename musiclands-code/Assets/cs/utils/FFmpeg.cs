using System.Collections;
using System.Collections.Generic;

using System.Diagnostics;
using UnityEngine;

public class FFmpeg : MonoBehaviour {
  
  private static string ffmpegExec = "/usr/bin/ffmpeg";
  
  // class that makes sure we run correct python commands on correct platforms
  public static int Convert(string filenameIn, string filenameTarget){
    
    // things are different on different operating systems, so on the first python script that we run, 
    // we also check what OS we're on and handle things appropriately
    if (ffmpegExec == "") {
      // todo: read executable name from settings
      ffmpegExec = "/usr/bin/ffmpeg";
    }
    
    
    Process proc = new Process();
    proc.StartInfo.FileName = ffmpegExec;
    proc.StartInfo.Arguments = "-i " + filenameIn + " " + filenameTarget + " -y";
    proc.Start();
    
    proc.WaitForExit();
    print("exit code? " + proc.ExitCode);
    return proc.ExitCode;
  }
}
