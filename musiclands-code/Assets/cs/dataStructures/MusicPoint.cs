using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicPoint {
  public float x;
  public float y;
  public int  id;
  private string meta;
  
  public string artist;
  public string title;
  public string album;
  public string genre;
  
  public MusicPoint(int id, float x, float y){
    this.x = x;
    this.y = y;
    this.id = id;
  }
  
  public void SetMeta(string meta){
    this.meta = meta;
  }
  
  public void SetPoint(float x, float y){
    this.x = x;
    this.y = y;
  }
  
  public void SetId(int id){
    this.id = id;
  }
  
  public float GetX(){
    return this.x;
  }
  
  public float GetY(){
    return this.y;
  }
  
  public int GetId(){
    return this.id;
  }
  
  public string GetMeta(){
    return this.meta;
  }
}
