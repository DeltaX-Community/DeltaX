import { Client } from "rpc-websockets";
import { ref, Ref } from 'vue'

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
        this.KnownTopics = [];
        this.IsConnected = false;
        this.Topics = ref({} as Array<TopicDto>)
        this.Topics.value = []

        this.on('open', this.RtOnOpen);
        this.on('close', this.RtOnClose);
        this.on('rt.notify.tags', this.RtOnNotifyTags);
    }

    public IsConnected: boolean;
    public KnownTopics: Array<string>;
    public Topics: Ref<Array<TopicDto>>;

    private async RtOnClose() {
        console.log("rt on close:");
        this.IsConnected = false;
        this.emit("rt.disconnected");
    }

    private async RtOnOpen(result: unknown) {
        console.log("rt on open:", result);
        this.IsConnected = true;
        await this.RtGetTopics()
        // await this.RtSubscribe(this.KnownTopics)
        this.emit("rt.connected");
    }

    public async RtGetTopics() {
        console.log("call RtGetTopics")
        const tops = await this.call('rpc.rt.get_topics', [], this.timeout) as Array<string>;
        console.log("call RtGetTopics result", tops);
        this.KnownTopics = tops;
        return this.KnownTopics;
    }

    private RtOnNotifyTags(result: { tags: Array<TopicDto> }) {
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

    public async RtSubscribe(topics: Array<string>) {
        // : Promise<unknown>         

        console.log("rpc.rt.subscribe topics", topics);
        const resultTags = await this.call('rpc.rt.subscribe', topics, this.timeout) as { tags: Array<TopicDto> };
        console.log("rpc.rt.subscribe resultTags", resultTags);

        this.RtOnNotifyTags(resultTags);
    }

    public async RtAddSubscribe(topics: Array<string>) {

        // si todos los tags a agregar estan subscritos no invoco el comando
        if (topics.findIndex(t => this.Topics.value.findIndex(tag => tag.tagName == t) < 0) < 0) {
            return;
        }
        const topicsSet = Array.from(new Set(topics.concat(this.Topics.value.map(t => t.tagName))));

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
