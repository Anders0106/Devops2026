#!/bin/bash

for c in $(docker ps -q); do 
    IMAGE=$(docker inspect --format='{{.Config.Image}}' $c)
    echo "Container: $c  Image: $IMAGE"
    docker exec $c id
    echo ""
done
