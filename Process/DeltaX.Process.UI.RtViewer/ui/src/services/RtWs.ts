import { Client } from "rpc-websockets";
import Vue from "vue";

const reactive = new Vue({
    data: function () {
        return {
            IsConnected: false,
            Topics: {} as { [name: string]: TopicDto },
            KnownTopics: {} as Array<string>
        };
    },
});

export interface TopicDto {
    tagName: string;
    status: boolean;
    updated: Date;
    value: string | number;
}

export class RealTimeWebSocket extends Client {
    private timeout: number;

    constructor(
        address?: string,
        options?: {
            autoconnect?: boolean;
            reconnect?: boolean;
            reconnect_interval?: number;
            max_reconnects?: number;
        }) {
        super(address, options);

        this.timeout = 10000;
        reactive.KnownTopics = [];
        reactive.IsConnected = false;
        reactive.Topics = {};

        this.on('open', this.RtOnOpen);
        this.on('close', this.RtOnClose);
        this.on('rt.notify.tags', this.RtOnNotifyTags);
    }

    public get IsConnected(): boolean {
        return reactive.IsConnected;
    }

    public get Topics(): { [name: string]: TopicDto } {
        return reactive.Topics
    }

    public get TopicsName(): Array<string> {
        return Object.keys(reactive.Topics);
    }

    public get KnownTopics(): Array<string> {
        return reactive.KnownTopics
    }

    private async RtOnClose() {
        console.log("rt on close:");
        reactive.IsConnected = false;
        this.emit("rt.disconnected");
    }

    private async RtOnOpen(result: unknown) {
        console.log("rt on open:", result);
        reactive.IsConnected = true;
        await this.RtGetTopics()
        // await this.RtSubscribe(this.KnownTopics)
        this.emit("rt.connected");
    }

    public async RtGetTopics() {
        console.log("call RtGetTopics")
        const tops = await this.call('rpc.rt.get_topics', [], this.timeout) as Array<string>;
        console.log("call RtGetTopics result", tops);
        reactive.KnownTopics = tops;
        return this.KnownTopics;
    }

    private RtOnNotifyTags(result: { tags: Array<TopicDto> }) {
        // console.log("rt notify.tags result.tags:", result.tags);  
        result.tags.forEach(tag => {
            tag.updated = new Date(tag.updated);
            Vue.set(reactive.Topics, tag.tagName, tag);
        });
    }

    public async RtSubscribe(topics: Array<string>) {
        // : Promise<unknown>         

        console.log("rpc.rt.subscribe topics", topics);
        const resultTags = await this.call('rpc.rt.subscribe', topics, this.timeout) as { tags: Array<TopicDto> };
        console.log("rpc.rt.subscribe resultTags", resultTags);

        this.RtOnNotifyTags(resultTags);
    }

    public async RtAddSubscribe(topics: Array<string>) {

        // si todos los tags a agregar estan subscritos no invoco el comando
        if (topics.findIndex(t => this.TopicsName.findIndex(tag => tag == t) < 0) < 0) {
            return;
        }

        const topicsSet = Array.from(new Set(topics.concat(Object.keys(reactive.Topics))));

        console.log("******** RtAddSubscribe topics", topicsSet);
        const resultTags = await this.call('rpc.rt.subscribe', topicsSet, this.timeout) as { tags: Array<TopicDto> };
        console.log("RtAddSubscribe resultTags", resultTags);

        this.RtOnNotifyTags(resultTags);
    }

    public async RtSetValues(topicValue: Array<{ topic: string; value: string | number | unknown }>) {
        console.log("send RtSetValues", topicValue);
        const result = await this.call('rpc.rt.set_value', topicValue, this.timeout) as boolean;
        console.log("receive RtSetValues", result);

        const toSubscribe = topicValue.map(t => t.topic);
        this.RtAddSubscribe(toSubscribe);
        return result;
    }

}

const scheme = document.location.protocol === "https:" ? "wss" : "ws";
const port = document.location.port ? ":" + document.location.port : "";
const connectionUrl = process.env.NODE_ENV === 'development'
    ? "ws://localhost:5011/rt"
    : scheme + "://" + document.location.hostname + port + "/rt";

// const connectionUrl = "ws://127.0.0.1:5050/ws";
const RtWs = new RealTimeWebSocket(connectionUrl);

export default RtWs;
