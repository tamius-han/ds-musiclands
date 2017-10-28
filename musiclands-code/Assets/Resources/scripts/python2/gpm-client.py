import sys
import rpyc

if __name__ == '__main__':
  conn = rpyc.connect("localhost", 8899)
  gpm = conn.root
  
  # no argparse because reasons
  args = sys.argv;
  
  if args[1] == "login":
    if len(args) == 5:
      print gpm.gpm_login(args[2], args[3], args[4])
    else:
      print gpm.gpm_login_nodeviceid(args[2], args[3])
  
  elif args[1] == "logout":
    print gpm.gpm_logout()
  
  elif args[1] == "find":
    print gpm.gpm_search_get_id(args[2])
    
  elif args[1] == "find_genre":
    print gpm.gpm_search_get_genre_and_id(args[2])
  
  elif args[1] == "get_stream":
    print gpm.gpm_get_stream_url(args[2])

  elif args[1] == "thumbs_up":
    print gpm.gpm_give_thumbs_up(args[2])
  
  elif args[1] == "save":
    print gpm.gpm_add_to_playlist(args[2])
  
  elif args[1] == "shutdown":
    print gpm.shutdown()

  else:
    print "unknown command"
