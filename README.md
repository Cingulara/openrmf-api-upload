![.NET Core Build and Test](https://github.com/Cingulara/openrmf-api-upload/workflows/.NET%20Core%20Build%20and%20Test/badge.svg)

# openrmf-api-upload

> *As of version 1.8, this functionality is moved into the openrmf-api-read project to reduce the footprint and number of components.*

This is the OpenRMF Upload API for uploading a CKL file. It has two calls and talks to the 
same database for the read, save, and upload APIs and message clients associated.

* POST to / to save a new checklist
* PUT to /{id} to update a new checklist content but keep the rest in tact
* /swagger/ gives you the API structure.

## Making your local Docker image
* make build
* make latest

## creating the user
* ~/mongodb/bin/mongo 'mongodb://root:myp2ssw0rd@localhost'
* use admin
* db.createUser({ user: "openrmf" , pwd: "openrmf1234!", roles: ["readWriteAnyDatabase"]});
* use openrmf
* db.createCollection("Artifacts");
* db.Artifacts.createIndex({ systemGroupId: 1 })
* db.Artifacts.createIndex({ stigType: 1 })
* db.Artifacts.createIndex({ stigRelease: 1 })
* db.Artifacts.createIndex({ version: 1 })
* db.createCollection("SystemGroups");
* db.SystemGroups.createIndex({ title: 1 })

## connecting to the database collection straight
~/mongodb/bin/mongo 'mongodb://openrmf:openrmf1234!@localhost/openrmf?authSource=admin'

## Messaging Platform
Using NATS from Synadia to have a messaging backbone and eventual consistency. Currently publishing to these known items:
* openrmf.upload.new with payload (new Guid Id)
* openrmf.upload.update with payload (new Guid Id)

More will follow as this expands for auditing, logging, etc.

### How to run NATS
* docker run --rm --name nats-main -p 4222:4222 -p 6222:6222 -p 8222:8222 nats
* this is the default and lets you run a NATS server version 1.2.0 (as of 8/2018)
* just runs in memory and no streaming (that is separate)