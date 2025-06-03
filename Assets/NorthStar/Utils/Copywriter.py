# Copyright (c) Meta Platforms, Inc. and affiliates.
import os

COPYRIGHT_STR = "// Copyright (c) Meta Platforms, Inc. and affiliates."

def RunOnFile(file):
    with open(file, "r+") as f:
        if (COPYRIGHT_STR in f.readline()):
            return
        f.seek(0)
        data = f.read()
        f.seek(0)

        f.write(f"{COPYRIGHT_STR}\n{data}")
        f.truncate()


scanQueue = []
scanQueue.append(os.getcwd()+"/NorthStar/Scripts")
while len(scanQueue) > 0:
    lookAt = scanQueue.pop()
    if(os.path.isdir(lookAt)):
        for i in os.scandir(lookAt):
            scanQueue.append(i.path)
    elif(os.path.isfile(lookAt)):
        ext = lookAt.split(".")[-1]
        if(ext == "cs"):
            print(lookAt)
            RunOnFile(lookAt)
        