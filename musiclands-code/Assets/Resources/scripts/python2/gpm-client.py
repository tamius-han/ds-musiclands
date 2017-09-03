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
  
  elif args[1] == "remove_new":
    print gpm.gpm_delete_if_new(args[2])

  elif args[1] == "delete_new":
    print gpm.gpm_delete_if_new(args[2])

  elif args[1] == "delete_force":
    print gpm.gpm_delete_force(args[2])

  elif args[1] == "clean":
    print gpm.gpm_clean()

  elif args[1] == "clean_all":
    print gpm.gpm_clean_all()

  elif args[1] == "shutdown":
    print gpm.shutdown()

  elif args[1] == "newly_added":
    file=gpm.newly_added()
    for line in file:
      print line

  else:
    print "unknown command"
