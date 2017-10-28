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
  saved_songs_pid = ""   # ID of the playlist for songs we want to save while exploring
  
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
      global saved_songs_pid
      newly_added = []
      
      
      # fetch all the playlist, save ID of the one we'll use
      playlists = api.get_all_playlists()
      for p in playlists:
        if p['name'] == "La la landscape | saved songs" :
          saved_songs_pid = p['id']
          break     # although friendly reminder: 'name' doesn't have to be unique.
      
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
      global saved_songs_pid
      saved_songs_pid = "";
      newly_added = []
      
            # fetch all the playlist, save ID of the one we'll use
      playlists = api.get_all_playlists()
      for p in playlists:
        if p['name'] == "La la landscape | saved songs" :
          saved_songs_pid = p['id']
          break     # although friendly reminder: 'name' doesn't have to be unique.
      
      print "login sucfcessful. id of saved songs playlist: " + saved_songs_pid
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
  
  
  def exposed_shutdown(self):
    print "shutting down"
    main.just_quit = True
    sys.exit();
  
  
  def exposed_gpm_give_thumbs_up(self, sid):
    print "giving thumbs up to song " + sid
    return api.rate_songs(sid, 5)
  
  
  def exposed_gpm_add_to_playlist(self, sid):
    global saved_songs_pid;
    print "saving song with id: " + sid + " to playlist " + saved_songs_pid + " ..."
    
    # check if saved_songs_pid is empty. If it's empty, we'll have to create a new playlist
    if saved_songs_pid == "" :
      print "we dont have playlist, so we'll create one."
      saved_songs_pid = api.create_playlist("La la landscape | saved songs", "Songs that you saved while exploring musical landscape");
    
    # now we 100% know that we have a playlist to call our own
    return api.add_songs_to_playlist(saved_songs_pid, sid)
  
  
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
