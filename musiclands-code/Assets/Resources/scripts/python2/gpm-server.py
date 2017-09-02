# google play music client service
# 
# Is a (persistent) client for Google Play Music that can be talked to by other processes
# using gpm-client.py. 
# 

import time
from gmusicapi import Mobileclient

import rpyc 

# service definition here
class GpmService(rpyc.Service):
  
  api   = Mobileclient()
  mylib = [];
  
  def exposed_gpm_login_nodeviceid(self, uname, passwd):
    global api
    api = Mobileclient();
    status = api.login(uname, passwd, Mobileclient.FROM_MAC_ADDRESS)
    
    if status == True:
      global mylib
      mylib = api.get_all_songs()
    
    return status
    
  def exposed_gpm_login(self, uname, passwd, deviceId):
    global api
    api = Mobileclient();
    status = api.login(uname, passwd, deviceId)
    
    if status == True:
      global mylib
      mylib = api.get_all_songs()
    
    return status
  
  def exposed_gpm_logout(self):
    return api.logout()
  
  def exposed_gpm_search_get_id(self, query):
    candidates = api.search(query)
    #todo: check if artist/song match, return best match
    if candidates['song_hits'] == []:
      return "NO_HITS"
    
    if 'storeId' in candidates['song_hits'][0]['track'].keys():
      return api.add_store_tracks(candidates['song_hits'][0]['track']['storeId'])
    
    return "NO_STORE_ID"
  
  def exposed_gpm_get_stream_url(self, songId):
    return api.get_stream_url(songId)
  
  def exposed_gpm_delete_if_new(self, songId):
    for track in mylib:
      if track['id'] == songId:
        return "SONG_EXISTED"
    
    api.delete_songs(songId)
    return "SONG_DELETED"

# start service server here

from rpyc.utils.server import ThreadedServer
from threading import Thread

server = ThreadedServer(GpmService, port = 8899)
t = Thread(target = server.start)
t.daemon = True
t.start();

while True:
  time.sleep(5);
