# Docker setup

- One volume for data that can be deleted whenever
- One volume just for the sqlite NadekoBot.db that should be kept around
- credentials.json needs point to db/NadekoBot.db
- To re-deploy, remove the data volume and rebuild the container
