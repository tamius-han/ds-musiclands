# google play music client service
# 
# Is a (persistent) client for Google Play Music that can be talked to by other processes
# using gpm-client.py. 
# 

import time
import sys
from gmusicapi import Mobileclient

class TheresGottaBeABetterWay():
  just_quit=False

import rpyc 

# service definition here
class GpmService(rpyc.Service):
  
  api   = Mobileclient()
  mylib = [];
  
  # we need something to manually clean the library in case of a mess
  newly_added = [];
  
  def exposed_ping(self):
    return true;
  
  def exposed_gpm_login_nodeviceid(self, uname, passwd):
    print "trying to log wihtout device id"
    global api
    api = Mobileclient()
    status = api.login(uname, passwd, Mobileclient.FROM_MAC_ADDRESS)
    
    if status == True:
      global newly_added
      global mylib
      mylib = api.get_all_songs()
      newly_added = []
      print "login successful."
    else:
      print "login failed."
    
    return status
    
  def exposed_gpm_login(self, uname, passwd, deviceId):
    print "trying to log in as " + uname + " with deviceId"
    global api
    api = Mobileclient();
    status = api.login(uname, passwd, deviceId)
    
    if status == True:
      global mylib
      global newly_added
      newly_added = []
      mylib = api.get_all_songs()
      print "login sucfcessful."
    else:
      print "login failed."
    
    return status
  
  def exposed_gpm_logout(self):
    print "logged out"
    return api.logout()
  
  def exposed_gpm_search_get_id(self, query):
    candidates = api.search(query)
    #todo: check if artist/song match, return best match
    if candidates['song_hits'] == []:
      return "NO_HITS"
    
    if 'storeId' in candidates['song_hits'][0]['track'].keys():
      return candidates['song_hits'][0]['track']['storeId']
      #global newly_added;      
      #return api.add_store_tracks(candidates['song_hits'][0]['track']['storeId'])
      #print "added " + newsong[0] + " to the library."
      #newly_added.append(newsong[0])
      #return newsong[0] 
    
    return "NO_STORE_ID"
  
  def exposed_gpm_search_get_genre_and_id(self, query):
    candidates = api.search(query)
    #todo: same as gpm_search_get_id
    if candidates['song_hits'] == []:
      return 'NO_HITS'
    if 'storeId' in candidates['song_hits'][0]['track'].keys():
      global newly_added
      newsong = api.add_store_tracks(candidates['song_hits'][0]['track']['storeId'])
      newly_added.append(newsong[0])
      return (candidates['song_hits'][0]['track']['genre'] + ":|:|:" + newsong[0])

  def exposed_gpm_get_stream_url(self, songId):
    return api.get_stream_url(songId)
  
  def exposed_gpm_delete_if_new(self, songId):
    for track in mylib:
      if track['id'] == songId:
        return "SONG_EXISTED"
    
    api.delete_songs(songId)
    print "deleted " + songId + " from library as it was added by our script."
    return "SONG_DELETED"
  
  def exposed_gpm_delete_force(self, songId):
    return api.delete_songs(songId)
  
  def exposed_gpm_clean(self):
    print "removing newly added songs from library"
    global newly_added
    for songId in newly_added:
      match = False;
      for track in mylib:
        if track['id'] == songId:
          match = True;
      if match != True:
        api.delete_songs(songId)
    
    print "newly added songs removed from library"
    return "It is done."
  
  def exposed_gpm_clean_all(self):
    global newly_added
    for songId in newly_added:
      api.delete_songs(songId)
    
    print "all songs played in current session have been removed from gpm library"
    return "It is done."
  
  def exposed_shutdown(self):
    print "shutting down"
    main.just_quit = True
    sys.exit();
  
  
  def exposed_newly_added(self):
    return newly_added;
  
# start service server here

from rpyc.utils.server import ThreadedServer
from threading import Thread

server = ThreadedServer(GpmService, port = 8899)
t = Thread(target = server.start)
t.daemon = True
t.start();

main = TheresGottaBeABetterWay();
while True:
  if main.just_quit:
    sys.exit()
  time.sleep(5);
