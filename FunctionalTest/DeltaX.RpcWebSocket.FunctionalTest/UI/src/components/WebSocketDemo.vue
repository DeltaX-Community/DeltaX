<template>
  <div class="hello">
    <h1>{{ msg }}</h1>
    <p>
      For a guide and recipes on how to configure / customize this project,<br />
      check out the
      <a href="https://cli.vuejs.org" target="_blank" rel="noopener"
        >vue-cli documentation</a
      >.
    </p>
  </div>
</template>

<script lang="ts">
import { Options, Vue } from 'vue-class-component';
const connectionUrl = "ws://127.0.0.1:5050/ws";
//// const client = new WebSocket(connectionUrl);
//// 
//// client.onerror = function (event) {
////   console.log("----- on error", event);
////   client.close();
//// };
//// 
//// client.onclose = function (event) {
////   console.log("----- on close", event);
////   // self.connected = false;
////   // setTimeout(function() {
////   //   console.log("reconnect...");
////   //   self.connect();
////   // }, 10000);
//// };
//// 
//// client.onopen = function (event) {
////   console.log("----- on open", event);
//// };
//// 
//// client.onmessage = function (event) {
////   // const data = JSON.parse(event.data);
////   console.log(event, event.data
////   
////   );
//// }

import { Client } from "rpc-websockets";

const ws = new Client(connectionUrl)

ws.on('open', function () {

  // subscribe to receive an event
  // ws.subscribe('feedUpdated')
  // ws.on('feedUpdated', function (result) {
  //   console.log("feedUpdated", result);
  // });

  setTimeout(() => {
    console.log('call rpc.rt.set_value')
    ws.call('rpc.rt.set_value', [
      { topic: "feedUpdated", value: 123456 },
      { topic: "powerTag", value: 5454 }
    ], 10000).then(function (result) {
      console.log("Sum result", result);
    })
  }, 5000);

  ws.on('rt.notify.tags', function (result) {
    console.log("rt.notify.tags result:", result);
  });

  ws.call('rpc.rt.subscribe', ["{feedUpdated}", "{powerTag}"], 10000)
    .then(function (result) {
      console.log("rpc.rt.subscribe result", result);
    })
    .catch(function (error) {
      console.log("rpc.rt.subscribe error", error);
    });

  // ws.call('Sum', [5, 3], 10000)
  //   .then(function (result) {
  //     console.log("Sum result", result);
  //   })
  //   .catch(function (error) {
  //     console.log("Sum error", error);
  //   });

});


@Options({
  props: {
    msg: String
  }
})
export default class HelloWorld extends Vue {
  msg!: string
}
</script>

<!-- Add "scoped" attribute to limit CSS to this component only -->
<style scoped>
h3 {
  margin: 40px 0 0;
}
ul {
  list-style-type: none;
  padding: 0;
}
li {
  display: inline-block;
  margin: 0 10px;
}
a {
  color: #42b983;
}
</style>
