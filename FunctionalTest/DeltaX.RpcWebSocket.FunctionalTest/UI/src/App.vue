<template>
<div class="content-center  items-center">
   
  <WebSocketDemo class="grid" msg="Welcome to Your Vue.js + TypeScript App" />
</div>
  
</template>

<script lang="ts">
import { Options, Vue } from 'vue-class-component';
import WebSocketDemo from './components/WebSocketDemo.vue';

@Options({
  components: {
    WebSocketDemo,
  },
})
export default class App extends Vue { }
</script>
 
