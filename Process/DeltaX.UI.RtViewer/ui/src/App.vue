<template>
  <v-app>
    <v-main>
      <RtViewTable />
    </v-main>
  </v-app>
</template>
 
<script lang="ts">
import { Component, Vue } from 'vue-property-decorator';
import RtViewTable from './components/RtViewTable.vue';

@Component({
  components: {
    RtViewTable,
  },
})
export default class App extends Vue { }
</script>

