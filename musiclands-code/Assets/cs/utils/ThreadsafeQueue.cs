using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThreadsafeQueue<T> {
  
  TsqElement<T> first = null;
  TsqElement<T> last = null;
  
  object _lock_enqueue = new object();
  object _lock_dequeue = new object();
  
  public void Enqueue(T data, int tag){
    TsqElement<T> nextElement = new TsqElement<T>(data, tag);
    
    lock(this._lock_enqueue) {
      if(first == null){
        lock(this._lock_dequeue){
          //we need this lock because we're modifying the first element
          this.first = nextElement;
          this.last = nextElement;
          return;
        }
      }
      
      this.last.next = nextElement;
      this.last = nextElement;
    }
  }
  
  public T Dequeue(){
    lock(this._lock_dequeue){
      if(this.first == null)
        return default(T);
      
      T data = this.first.element;
      this.first = this.first.next;
      
      // since we check for empty queue with first == null before doing anything, we don't have to 
      // set last to null when the queue is depleted (because all functions check if first is null)
      
      return data;
    }
  }
  
  public T Peek(){
    lock(this._lock_dequeue){ // we don't want a dequeue happen mid-executing this function
      if(this.first == null)
        return default(T);
      
      return this.first.element;
    }
  }
  
  public bool IsEmpty(){
    return this.first == null;
  }
  
  public void Drop(){
    //drops all elements
    
    lock(this._lock_dequeue){
      lock(this._lock_enqueue){
        this.first = null;
      }
    }
  }
  
  public void Drop(int tag){
    //Drops all elements that have this tag
    
    lock(this._lock_dequeue){
      if(this.first == null)    // this is equally inacurrate if we keep it in a lock or not, cos dequeue can't happen anymore
        return;
      
      lock(this._lock_enqueue){        
        TsqElement<T> iterator = this.first;
        while(iterator.next != null){
          if (iterator.next.tag == tag){
            iterator.next = iterator.next.next;
            continue;  // let's not advance the iterator in that case
          }
          iterator = iterator.next;
        }
        // we skipped the first one
        if(this.first.tag == tag)
          first = first.next;
      }
    }
  }
  
  public void Keep(int tag){
    //Discards elements that have a tag different than this, basically reverse Drop(tag)
    
    lock(this._lock_dequeue){
      if(this.first == null)      // this is equally inacurrate if we keep it in a lock or not, cos dequeue can't happen anymore
        return;
      
      lock(this._lock_enqueue){        
        TsqElement<T> iterator = this.first;
        while(iterator.next != null){
          if (iterator.next.tag != tag){
            iterator.next = iterator.next.next;
            continue;  // let's not advance the iterator in that case
          }
          iterator = iterator.next;
        }
        // we skipped the first one
        if(this.first.tag != tag)
          first = first.next;
      }
    }    
  }
  
  public void PrioritizeTag(int tag){
    //moves all elements with specified tag to beginning of the queue
    lock(this._lock_dequeue){
      if(this.first == null)
        return;
      
      lock(this._lock_enqueue){
        TsqElement<T> iterator = this.first;
        TsqElement<T> pivot;
        
        // this time around, we don't even need to check the first element. If it has tag —> ok, it'll be before all elements with
        // a different tag. If it doesn't have matching tag -> all elements with matching tag will come in front of it
        while(iterator.next != null){
          if(iterator.next.tag == tag){
            pivot = iterator.next;
            pivot.next = this.first;
            this.first = pivot;
            
            iterator.next = iterator.next.next;
            continue;
          }
          iterator = iterator.next;
        }
      }
    }
  }
  
}
class TsqElement<T> {
  public T element;
  public TsqElement<T> next;
  public int tag;
  
  public TsqElement(T data, int tag){
    this.element = data;
    this.tag = tag;
    this.next = null;
  }
}
