# Transcription Service

## Description
- Service for retrieving messages from Rabbitmq queue. 
- Messages are then passed to connected frontends using a Signalr connection.

## Usage Guide
``` cmd
# Start Rabbitmq Management
kubectl apply -f ./rabbitmqdeploy.yaml  

# Build and run the service container
REM TranscriptionService
docker image build --file ./Dockerfile_TranscriptionService -t transcriptionservice .
docker container run -d -p 5000:80 --name TranscriptionService transcriptionservice

REM NERService
docker image build --file ./Dockerfile_NERService -t nerservice .
docker container run -d -p 5000:80 --name NERService nerservice

# Navigate to the Angular UI project folder and execute the below command
ng serve
```
- Your payload when entering messages to the Rabbitmq Queue should be a valid JSON Object

``` javascript
//E.g.
{
 "property1": "some value"   
}
```

## Using Kong 
- Setting up Kong with helm
``` cmd
.\helm.exe repo add kong https://charts.konghq.com
.\helm.exe repo update
.\helm.exe install kong/kong --generate-name --set ingressController.installCRDs=false
```
- Running kubernetes deployment files
``` cmd
kubectl apply -f .\transcriptionservice.yaml
kubectl apply -f .\nerservice.yaml
kubectl apply -f .\ingress.yaml
```