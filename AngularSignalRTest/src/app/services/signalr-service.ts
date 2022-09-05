import { Injectable } from '@angular/core';
import * as signalR  from '@microsoft/signalr';

import { environment } from '@environments/environment'

@Injectable({
  providedIn: 'root'
})
export class SignalRService {

  connection: { [connection: string]: signalR.HubConnection };

  constructor() { 
    this.connection = {};
  }

  private async getConnection(connection: string, url: string): Promise<signalR.HubConnection | undefined> {

    if(this.connection.hasOwnProperty(connection)) {
      return this.connection[connection];
    }

    try {

      const build_connection = 
        new signalR.HubConnectionBuilder()
        .withUrl(url, {
          skipNegotiation: true,
          transport: signalR.HttpTransportType.WebSockets
        })
        .build();

      await build_connection.start();
      
      return this.connection[connection] = build_connection;

    }
    catch(e) {
      console.error(`Failed to build connection with ${url}`, e);
      return;
    }
  }

  async sendToSignalR(data: {
      methodName: string;
      payload: string;
      connection: string;
    }
  ) {

    if(!environment.signalrURL.hasOwnProperty(data.connection)) return;

    let connectionUrl = environment.signalrURL[data.connection];

    let _connection = await this.getConnection(data.connection, connectionUrl);

    if(!_connection) return;
    
    await _connection.invoke(data.methodName, 'angular-signalr-test', data.payload);

  }

  async respondToSignalR(
    data: { 
      on: string; 
      connection: string;
      callback: (sender: string, res: string) => void; 
    } 
  ) {

    if(!environment.signalrURL.hasOwnProperty(data.connection)) return;

    let connectionUrl = environment.signalrURL[data.connection];

    let _connection = await this.getConnection(data.connection, connectionUrl);

    if(!_connection) return;
    
    _connection.on(data.on, data.callback)

  }

}
