<template>
  <div class="hello">
    <h1>RtView</h1>
    <form>
      <label for="topicWrite">Topic Write:</label>
      <input id="topicWrite" type="text" v-model="topicWrite" />
      <label for="valueWrite">Value:</label>
      <input id="valueWrite" type="text" v-model="valueWrite" />
      <button @click.prevent="SetTag()">Set Tag</button>
    </form>
    <form>
      <label for="topicSubscribe">Topic Subscribe (Expression):</label>
      <input id="topicSubscribe" type="text" v-model="topicSubscribe" /> 
      <button @click.prevent="SubscribeTopic()">Subscribe Topic</button>
    </form>
    <table style="width: 100%">
      <thead>
        <th>Tag Name</th>
        <th>Status</th>
        <th>Value</th>
        <th>Updated</th>
      </thead>
      <tbody v-if="topics">
        <tr v-for="t in topics" :key="t.tagName">
          <td>{{ t.tagName }}</td>
          <td>{{ t.status }}</td>
          <td>{{ t.value }}</td>
          <td>{{ t.updated }}</td>
        </tr>
      </tbody>
    </table>
  </div>
</template>

<script lang="ts">
import { Options, Vue } from 'vue-class-component';
// const connectionUrl = "ws://127.0.0.1:5050/ws";
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

// import { Client } from "rpc-websockets";
import RtWs from "./RtWs";

const ws = RtWs;

ws.on('rt.connected', function () {
  console.log("open from vue (rt.connected)");
  ws.RtSubscribe(ws.KnownTopics);
});


setTimeout(() => {
  console.log('call RtSetValues')
  ws.RtSetValues([
    { topic: "feedUpdated", value: 123456 },
    { topic: "powerTag", value: 5454 },
    { topic: "setFromVue2", value: new Date() }
  ]).then(function (result) {
    console.log("Sum result", result);
  })
}, 2000);


@Options({
  props: {
    msg: String
  }
})
export default class HelloWorld extends Vue {
  msg!: string
  topics = RtWs.Topics
  topicWrite = ""
  valueWrite = ""
  topicSubscribe = ""

  SetTag() {
    RtWs.RtSetValues([{ topic: this.topicWrite, value: this.valueWrite }])
    this.valueWrite = ""
  }

  SubscribeTopic() {
    RtWs.RtAddSubscribe([this.topicSubscribe])
    this.topicSubscribe = "";
  }
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
