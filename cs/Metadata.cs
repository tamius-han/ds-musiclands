using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Metadata {
  private string artist;
  private string title;
  private string album;
  
  public Metadata(){
    this.artist = "";
    this.title = "";
    this.album = "";
  }
  
  public Metadata(string make_a_guess){
    string[] meta = make_a_guess.Split('-'); // will do good enough for small set.
    
    if(meta.Length == 0){
      this.artist = "";
      this.title = "";
      this.album = "";
    }
    else if(meta.Length == 1){
      this.artist = "";
      this.title = make_a_guess;
      this.album = "";
    }
    else if(meta.Length == 2){
      this.artist = meta[0];
      this.title = meta[1];
      this.album = "";
    }
    else{
      this.artist = meta[0];
      this.title = meta[1];
      this.album = meta[2];
    }
  }
  
  public Metadata(string artist, string title){
    this.artist = artist;
    this.title = title;
    this.album = "";
  }
  
  public Metadata(string artist, string title, string album){
    this.artist = artist;
    this.title = title;
    this.album = album;
  }
  
  public string ToString(){
    string ret = this.artist + " - " + this.title + " (" + this.album + ")";
    return ret;
  }
}
