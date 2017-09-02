using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Sdigital = 7digital, ampak c# ne dovoli številke na začetku

public class SdigitalConf : MonoBehaviour {

  static SdigitalKey _key = null;
  
  public static SdigitalKey GetKeySecret(){
    if(_key == null){
      LoadSdigitalKeySecret();
    }
    
    return _key;
  }
  
  public static void LoadSdigitalKeySecret(){
    try{
      string key, secret;
      StreamReader sr = new StreamReader(GlobalData.dataPath + "/Resources/conf/7digital", Encoding.Default);
      
      using(sr){
        key = sr.ReadLine();
        secret = sr.ReadLine();
      }
      
      _key = new SdigitalKey(key, secret);
    }
    catch(Exception e){
      Console.WriteLine("There's something wrong with 7digital key/secret file. Reconfigure via settings.", e.Message);
    }
  }
}

public class SdigitalKey {
  public string key;
  public string secret;
  
  public SdigitalKey(string k, string s){
    key = k;
    secret = s;
  }
}
