// configuration for gpm here

using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GpmConf : MonoBehaviour {
  
  public static bool gpmLoggedIn = false;
  public static string gpmUser = "";
  
  public static void InitGpm(){
    Python.RunScriptBg("gpm-server.py");
  }
  
  public static bool Login(){
    // WARN: this method will take a while to run, as logging in also fetches user's library
    // make sure to run as a thread inside a coroutine
    
    if(File.Exists(GlobalData.dataPath + "/Resources/conf/gpm")){
      try{
        
        string uname, pass, deviceId;
        StreamReader sr = new StreamReader(GlobalData.dataPath + "/Resources/conf/7digital", Encoding.Default);
        
        using(sr){
          uname = sr.ReadLine();
          pass = sr.ReadLine();
          deviceId = sr.ReadLine();
        }
        
        string loginSucc = Python.RunScriptGetStdout("gpm-client.py", "login " + uname + " " + pass + " " + deviceId);
        
        if(loginSucc == "True"){
          gpmUser = uname;
          gpmLoggedIn = true;
          return true;
        }
        return false;
      }
      catch(Exception e){
        Console.WriteLine("[GpmConf::Login] something went wrong", e.Message);
      }
    }
    
    
    return false;
  }
  
  public static string Find(string searchQuery){
    try{
      searchQuery = searchQuery.Replace("'", @"\'");
      string result = Python.RunScriptGetStdout("gpm-client.py", "find  \"" + searchQuery + "\"");
      return result;
    }
    catch(Exception e){
      return "GPM_ERROR";
    }
  }
  
  public static string[] FindGenre(string searchQuery){
    searchQuery = searchQuery.Replace("'", @"\'");
    string result = Python.RunScriptGetStdout("gpm-client.py", "find  \"" + searchQuery + "\"");
    if(result == "NO_HITS" || result == "NO_STORE_HITS")
      return null;
    string[] gibgenre = result.Split(new string[] {":|:|:"}, 2, StringSplitOptions.None);
    return gibgenre;
  }
  
  public static string GetStream(string songId){
    return Python.RunScriptGetStdout("gpm-client.py", "get_stream " + songId);
  }
  
  public static void RemoveNew(string songId){
    Python.RunScript("gpm-client.py", "remove_new " + songId);
  }
  
  public static void RemoveAllNew(){
    Python.RunScript("gpm-client.py", "clean");
  }
  
}

public class GpmLogin {
  public string key;
  public string secret;
  public string deviceId;
  
  public GpmLogin(string k, string s, string d){
    key = k;
    secret = s;
    deviceId = d;
  }
}
