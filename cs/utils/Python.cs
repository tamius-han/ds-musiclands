using System.Collections;
using System.Collections.Generic;

using System.Diagnostics;
using UnityEngine;

public class Python : MonoBehaviour {

  private static string pythonExec = "";
  private static string pythonPath = "/Resources/scripts/python2/";

  
  // class that makes sure we run correct python commands on correct platforms
  public static int RunScript(string name){
    return RunScript(name, "", false);
  }
  
  public static int RunScriptBg(string name){
    return RunScript(name, "", true);
  }
  
  public static int RunScript(string name, string args){
    return RunScript(name, args, false);
  }
  
  public static int RunScriptBg(string name, string args){
    return RunScript(name, args, true);
  }
  
  public static int RunScript(string name, string args, bool background){
    
    // things are different on different operating systems, so on the first python script that we run, 
    // we also check what OS we're on and handle things appropriately
    if (pythonExec == "") {
      // todo: read executable name from settings
      pythonExec = "/usr/bin/python2";
    }
    
//     print("trying to run script. python executable: " + pythonExec + "; script+args: " + (GlobalData.dataPath + pythonPath + name) + " " + args );
    
    Process proc = new Process();
    proc.StartInfo.FileName = pythonExec;
    proc.StartInfo.Arguments = (GlobalData.dataPath + pythonPath + name) + " " + args;
    proc.Start();
    
    if(! background){
      proc.WaitForExit();
//       print("exit code? " + proc.ExitCode);
      return proc.ExitCode;
    }
    
    return 0;
  }
  
  public static string RunScriptGetStdout(string name, string args){
    // things are different on different operating systems, so on the first python script that we run, 
    // we also check what OS we're on and handle things appropriately
    if (pythonExec == "") {
      // todo: read executable name from settings
      pythonExec = "/usr/bin/python2";
    }
    string output;
    
    Process proc = new Process();
    proc.StartInfo.FileName = pythonExec;
    proc.StartInfo.UseShellExecute = false;
    proc.StartInfo.RedirectStandardOutput = true;
    proc.StartInfo.Arguments = (GlobalData.dataPath + pythonPath + name) + " " + args;
    proc.Start();
    
    output = proc.StandardOutput.ReadToEnd();
    
    proc.WaitForExit();
    
    return output;
  }
}
