#!/bin/bash

idFileIn=$1      # file containing all ids 
idFileOut=$2     # we'll write ids to this file

echo -n "" > $idFileOut
while read -r id ; do
  if [[ ${#id} -gt 2 ]] ; then
    if [[ ${#id} -gt 5 ]] ; then
      #mv "$inputFolder/$id.mp3.mrscore" "$inputFolder/${id:0:2}/${id:2:2}/$id.mp3.mrscore"
      echo "${id:0:2}/${id:2:2}/$id" >> $idFileOut
    else
      #mv $inputFolder/$id $inputFolder/${id:0:2}/$id
      echo "${id:0:2}/$id" >> $idFileOut
    fi
  else
    echo "$id" >> $idFileOut
  fi
  #if the filenames are too short, we don't move the files.
done < "$idFileIn"

echo ""
echo ""
echo ""
echo "DONE!"
echo ""
