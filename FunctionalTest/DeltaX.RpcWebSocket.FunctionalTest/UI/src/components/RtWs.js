import { Client } from "rpc-websockets";
import { ref } from 'vue';
export class RealTimeWebSocket extends Client {
    constructor(address, options) {
        super(address, options);
        this.timeout = 10000;
        this.KnownTopics = [];
        this.IsConnected = false;
        this.Topics = ref({});
        this.Topics.value = [];
        this.on('open', this.RtOnOpen);
        this.on('close', this.RtOnClose);
        this.on('rt.notify.tags', this.RtOnNotifyTags);
    }
    async RtOnClose() {
        console.log("rt on close:");
        this.IsConnected = false;
        this.emit("rt.disconnected");
    }
    async RtOnOpen(result) {
        console.log("rt on open:", result);
        this.IsConnected = true;
        await this.RtGetTopics();
        // await this.RtSubscribe(this.KnownTopics)
        this.emit("rt.connected");
    }
    async RtGetTopics() {
        console.log("call RtGetTopics");
        const tops = await this.call('rpc.rt.get_topics', [], this.timeout);
        console.log("call RtGetTopics result", tops);
        this.KnownTopics = tops;
        return this.KnownTopics;
    }
    RtOnNotifyTags(result) {
        console.log("rt notify.tags result.tags:", result.tags);
        // this.Topics.value.filter(t => result.tags.findIndex(tnew => tnew.tagName == t.tagName) < 0);
        result.tags.forEach(tag => {
            const idx = this.Topics.value.findIndex(t => t.tagName == tag.tagName);
            if (idx >= 0) {
                this.Topics.value[idx] = tag;
            }
            else {
                this.Topics.value.push(tag);
            }
        });
    }
    async RtSubscribe(topics) {
        // : Promise<unknown>         
        console.log("rpc.rt.subscribe topics", topics);
        const resultTags = await this.call('rpc.rt.subscribe', topics, this.timeout);
        console.log("rpc.rt.subscribe resultTags", resultTags);
        this.RtOnNotifyTags(resultTags);
    }
    async RtAddSubscribe(topics) {
        // si todos los tags a agregar estan subscritos no invoco el comando
        if (topics.findIndex(t => this.Topics.value.findIndex(tag => tag.tagName == t) < 0) < 0) {
            return;
        }
        const topicsSet = Array.from(new Set(topics.concat(this.Topics.value.map(t => t.tagName))));
        console.log("******** RtAddSubscribe topics", topicsSet);
        const resultTags = await this.call('rpc.rt.subscribe', topicsSet, this.timeout);
        console.log("RtAddSubscribe resultTags", resultTags);
        this.RtOnNotifyTags(resultTags);
    }
    async RtSetValues(topicValue) {
        console.log("send RtSetValues", topicValue);
        const result = await this.call('rpc.rt.set_value', topicValue, this.timeout);
        console.log("receive RtSetValues", result);
        const toSubscribe = topicValue.map(t => t.topic);
        this.RtAddSubscribe(toSubscribe);
        return result;
    }
}
const scheme = document.location.protocol === "https:" ? "wss" : "ws";
const port = document.location.port ? ":" + document.location.port : "";
const connectionUrl = process.env.NODE_ENV === 'development'
    ? "ws://localhost:5050/rt"
    : scheme + "://" + document.location.hostname + port + "/rt";
// const connectionUrl = "ws://127.0.0.1:5050/ws";
const RtWs = new RealTimeWebSocket(connectionUrl);
export default RtWs;
//# sourceMappingURL=RtWs.js.map