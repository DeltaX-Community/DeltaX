<template>
  <div class="block  m-6 p-0 rounded-xl border-4 border-green-900"> 
      <div class="  grid justify-center rounded-lg rounded-b-none">
        <h1 class="text-4xl font-extrabold m-4">RtView</h1>
      </div> 
    <div class="p-3  bg-red-700 rounded-xl rounded-t-none">
    <div class="grid">    
      <form class="my-1 grid-cols-4 bg-green-50 p-1 text-sm rounded-lg items-center grid-flow-row">
        <label class="" for="topicWrite">Topic:</label>
        <input class=" mx-1 p-1" id="topicWrite" type="text" v-model="topicWrite" />
        <label class="" for="valueWrite">Value:</label>
        <input class=" mx-1 p-1" id="valueWrite" type="text" v-model="valueWrite" />
        <button class="w-1/8 mx-1 p-1 px-4 text-white focus:to-blue-500 bg-blue-700 rounded-full"  @click.prevent="SetTag()">Set Tag</button>      
      </form>
      <form class="my-1 bg-green-50 p-1 text-sm rounded-lg items-center grid-flow-row">
        <label class="" for="topicSubscribe">Topic Subscribe (Expression):</label>
        <input class=" mx-1 p-1" id="topicSubscribe" type="text" v-model="topicSubscribe" /> 
        <button class="w-1/8 mx-1 p-1 px-4 text-white focus:to-blue-500 bg-blue-700 rounded-full" @click.prevent="SubscribeTopic()">Subscribe Topic</button>
      </form>
    </div>
    <table class="table-auto">
      <thead>
        <th class="font-black  text-green-900 rounded text-left">Tag Name</th>
        <th class="font-black  text-green-900 rounded text-left">Status</th>
        <th class="font-black  text-green-900 rounded text-left">Value</th>
        <th class="font-black  text-green-900 rounded text-left">Updated</th>
      </thead>
      <tbody v-if="topics">
        <tr v-for="t in topics" :key="t.tagName">
          <td class="text-left font-bold">{{ t.tagName }}</td>
          <td class="text-left">{{ t.status }}</td>
          <td class="text-right">{{ t.value }}</td>
          <td class="text-right">{{ t.updated }}</td>
        </tr>
      </tbody>
    </table>
      </div>
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
 