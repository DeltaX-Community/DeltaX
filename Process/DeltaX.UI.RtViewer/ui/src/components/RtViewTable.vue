<template>
  <div>
    <v-card class="row pa-2 ma-0" color="red">
      <div class="pa-2 col-md-4 col-lg-2 col-12">Flex X 1</div>
      <div class="pa-2 col-md-4 col-lg-2 col-12">Flex X 2</div>
      <div class="pa-2 col-md-4 col-lg-2 col-12">Flex X 3</div>
      <div class="pa-2 col-md-4 col-lg-2 col-12">
        <v-text-field
          v-model="searchTag"
          dense
          append-icon="mdi-magnify"
          label="Filter"
          single-line
          hide-details
        ></v-text-field>
      </div>
      <v-col sm="6" md="4">
        <v-card class="pa-2" outlined tile> Flex item 1 </v-card>
        <v-card class="pa-2" outlined tile> Flex item 2 </v-card>
        <v-spacer></v-spacer>
        <v-card class="pa-2" outlined tile> Flex item 3 </v-card>
      </v-col>
    </v-card>
    <v-card>
      <v-card-title>
        Real Time Viewer
        <v-icon v-if="RtWs.IsConnected" color="green" right large
          >mdi-lan-connect</v-icon
        >
        <v-icon v-else color="red" right large>mdi-lan-disconnect</v-icon>
        <v-spacer></v-spacer>
        <v-text-field
          v-model="searchTag"
          dense
          append-icon="mdi-magnify"
          label="Filter"
          single-line
          hide-details
        ></v-text-field>
      </v-card-title>

      <v-col class="my-4 pa-0">
        <v-row class="mx-0 pa-0">
          <v-col cols="12" md="3">
            <v-text-field
              outlined
              dense
              hide-details
              placeholder="Topic name to write!"
              v-model="topicWrite"
            ></v-text-field>
          </v-col>
          <v-col cols="12" md="3">
            <v-text-field
              outlined
              dense
              hide-details
              placeholder="Value to write!"
              v-model="valueWrite"
            ></v-text-field>
          </v-col>
          <v-col cols="12" md="1">
            <v-btn @click.prevent="SetTag()">Write</v-btn>
          </v-col>
        </v-row>
        <v-row class="ma-0 pa-0">
          <v-col cols="12" md="6">
            <v-text-field
              outlined
              dense
              hide-details
              placeholder="Subscribe Topic (Expression)!"
              v-model="topicSubscribe"
              @keypress.enter="SubscribeTopic()"
            ></v-text-field>
          </v-col>
          <v-col cols="12" md="2">
            <v-btn absolute @click.prevent="SubscribeTopic()">Subscribe</v-btn>
          </v-col>
        </v-row>
        <v-row class="ma-0 pa-0">
          <v-col cols="12" md="6">
            <v-text-field
              outlined
              dense
              hide-details
              placeholder="Topic for get History!"
              v-model="topicGetHistory"
            ></v-text-field>
          </v-col>
          <v-col cols="12" md="2">
            <v-btn absolute @click.prevent="GetHistory()">GetHistory</v-btn>
          </v-col>
        </v-row>
      </v-col>

      <v-data-table
        fix-header
        fixed-header
        height="calc(100vh - 290px)"
        dense
        single-select
        :search="searchTag"
        :headers="headers"
        sort-by="updated"
        sort-desc
        :items="Topics"
        :items-per-page="30"
      >
        <template v-slot:top> </template>

        <template v-slot:item.updated="{ item }">
          <span :style="getStyle(item.status)">
            {{ getDateFormat(item.updated) }}
          </span>
        </template>
      </v-data-table>
    </v-card>
  </div>
</template>


<script lang="ts">
import RtWs, { TopicDto } from "@/services/RtWs";
import { Component, Prop, Vue } from 'vue-property-decorator';

RtWs.on('rt.connected', function () {
  console.log("open from vue (rt.connected)");
  RtWs.RtSubscribe(RtWs.KnownTopics);
});

setTimeout(() => {
  console.log('call RtSetValues')
  RtWs.RtSetValues([
    { topic: "rtViewConnected", value: new Date() }
  ]).then(function (result) {
    console.log("RtSetValues result", result);
  })
}, 2000);

@Component
export default class RtViewTable extends Vue {
  @Prop() private title!: string;
  topicWrite = ""
  valueWrite = ""
  topicSubscribe = ""
  topicGetHistory = ""
  searchTag = ""
  RtWs = RtWs

  headers = [
    { text: 'Tag Name', value: 'tagName', sort: false },
    { text: 'Value', value: 'value', sort: false },
    { text: `Updated (${this.getLang()})`, value: 'updated', sort: false, width: "200" }
  ]

  public get Topics(): Array<TopicDto> {
    return Object.values(RtWs.Topics)
  }

  getStyle(status: boolean) {
    return {
      color: status ? 'green' : 'red'
    }
  }

  getLang() {
    if (navigator.languages != undefined)
      return navigator.languages[0];
    else
      return navigator.language;
  }

  getDateFormat(date: Date) {
    const offset = date.getTimezoneOffset()
    return new Date(date.getTime() - (offset * 60 * 1000)).toISOString().replace('T', ' ').slice(0, 23)
  }

  SetTag() {
    if (this.topicWrite != '' && this.valueWrite != '') {
      RtWs.RtSetValues([{ topic: this.topicWrite, value: this.valueWrite }])
      this.valueWrite = ""
    }
  }

  SubscribeTopic() {
    RtWs.RtAddSubscribe([this.topicSubscribe])
    // this.topicSubscribe = "";
  }

  GetHistory() {
    RtWs.HistoryGetTopic(this.topicGetHistory)
  }
}
</script>