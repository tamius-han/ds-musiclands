#!/bin/bash

# So, there's two major version of python: python 2 and python 3.
# Our scripts might want python 2, or they may want python 3
# The problem is that python 3 is not backwards compatible with python.
# Another problem is that name of the python2 executable is different on different distros.
#
# On Debian and Ubuntu, /usr/bin/python is generally python 2.
# On things like Arch and arch-based distros, /usr/bin/python is python 3, while python 2 is /usr/bin/python2
#
#
# =================================================================================================================================
#
# EXIT CODES (in case this script finds problems)
#
#           :: 99 (nein, nein) — no python installed. At all.
#           :: 92 (nein, py2) — there is python, but not python2 (the one we want)


# Here's the file we'll write to
runpy="./Assets/Resources/scripts/linux/runpy2.sh"

# get current major python version:
vpy=$(python --version)
if [[ $? -ne 0 ]] ; then
  # someone isn't a fan of snakes. This is a problem, but we still make a dummy file so that calls to python don't fail messily
  # and then exit with 1 to inform unity of a problem
  printf "#!/bin/bash\nexit 99\n" > $runpy  # Exit status of 99 (As in: nein, nein) basically stands for no python, both when 
  chmod +x $runpy                           # trying to run the dummy, as well as when we haven't managed to find python2
  exit 99;                                     
fi

vpy=$(echo $vpy | awk '{print $2}' | head -c 1)

if [[ $vpy -eq 2 ]] ; then
  # all is fine, we have python2
  printf "#!/bin/bash\npython \$@\n" > $runpy
else
  # in case someone only runs python3, with no python2
  which python2
  if [[ $? -eq 0 ]] ; then
    # python2 exists
    printf "#!/bin/bash\npython2 \$@\n" > $runpy
  else 
    # we didn't find python2
    printf "#!/bin/bash\nexit 92\n" > $runpy
    chmod +x $runpy
    exit 92
  fi
fi 

chmod +x $runpy
exit 0
