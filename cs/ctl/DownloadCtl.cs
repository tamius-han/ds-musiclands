using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class DownloadCtl : MonoBehaviour {

  public static int MAX_CONCURRENT_DOWNLOADS = 6;
  public static bool initiated = false;
  
  static Process[] concurrentDownloads =  new Process[MAX_CONCURRENT_DOWNLOADS];
  static int[] cdmp = new int[MAX_CONCURRENT_DOWNLOADS];
  
  static int headIndex = 0;
  static object addLock = new object();
  
  public static void StopLastDownload(Process p, int id){
    return;
//     lock(addLock){
//       print("|| Downloadctl || adding download!");
//       if(concurrentDownloads[headIndex] != null){
//         if(! concurrentDownloads[headIndex].HasExited){
//           // kill last process
//           concurrentDownloads[headIndex].Kill();
//           CacheOptions.DeleteCacheItem(id);  // uncache terminated download
//           print("|| Downloadctl || killing download of item with id " + id );
//         }
//       }
//       concurrentDownloads[headIndex] = p;
//       cdmp[headIndex] = id;
//       
//       headIndex = ++headIndex % MAX_CONCURRENT_DOWNLOADS;
//     }
  }
  
}
