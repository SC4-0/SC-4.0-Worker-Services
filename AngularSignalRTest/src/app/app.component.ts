import { Component } from '@angular/core';
import { SignalRService } from './services/signalr-service';
import { environment } from '@environments/environment';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent {
  title = 'AngularSignalRTest';

  constructor(private signalr: SignalRService) {

    /* http://localhost/transcription-service/Transcription */

    // this.signalr.respondToSignalR({
    //   on: "Transcription",
    //   connection: "transcriptionServiceUrl",
    //   callback: (sender: string, message: string) => {
    //     try{
    //       const data = JSON.parse(message);
    //       console.log(data);
    //     }
    //     catch(e) {
    //       console.error(e, `Message Received From Server: ${message}`);
    //     }  
    //   }
    // });


      this.signalr.sendToSignalR({
        methodName: 'ReceiveTranscription',
        payload: 'hello',
        connection: 'transcriptionServiceUrl'
      });


    var ws = new WebSocket(environment.signalrURL['transcriptionServiceUrl'].replace('http', 'ws'));

    function tryParseJSON(json: string){
      try {
        return JSON.parse(json);
      }
      catch(e){
        console.error('Unable to convert string into JSON object', json);
      }
    }

    ws.onmessage = function (e) {

      let data = e.data.substring(0, e.data.length - 1);

      data = tryParseJSON(data);
      
      if(!data.arguments) return;

      const on = data.target;
      const serverName = data.arguments[0];

      let payload = tryParseJSON(data.arguments[1]);

      console.log(payload);
      
    }

    
    ws.onopen = function (e) {
      // negotiate protocol 
      ws.send('{"protocol": "json", "version": 1}');
    }

  }
}
