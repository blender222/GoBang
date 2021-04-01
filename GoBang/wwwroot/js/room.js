let app = new Vue({
  el: '#board',
  data: {
    name: '', 
    faction: 'white',
    connection: new signalR.HubConnectionBuilder().withUrl("/goBangHub").build(),
    pieceArray: (function() {
      let arr = new Array(16);
      for (let i = 0; i <= 15; i++) {
        arr[i] = new Array(16);
      }
      return arr;
    })(),
  },
  methods: {
    piecePosition(i) {
      return {
        top: `${(Math.floor((i - 1) / 15) + 1) * 40}px`,
        left: `${(i % 15 == 0 ? 15 : i % 15) * 40}px`
      }
    },
    setPiece(e) {
      // e.target.classList.add('active');
      console.log(`x: ${e.target.x}`, `y: ${e.target.y}`);
      // 發送資料
      this.connection.invoke('SetPiece', e.target.x, e.target.y, this.faction).catch(function (err) {
        return console.error(err.toString());
      });
      // 禁止點擊
      
    },
  },
  created: function() {
    // 綁定座標 1,1 ~ 15,15
    this.$nextTick(() => {
      let arr = document.querySelectorAll('#piece-group .piece');
      for (let y = 1; y <= 15; y++) {
        for (let x = 1; x <= 15; x++) {
          this.pieceArray[y][x] = arr[(y - 1) * 15 + x - 1];
          this.pieceArray[y][x].x = x;
          this.pieceArray[y][x].y = y;
          this.pieceArray[y][x].addEventListener('click', this.setPiece);
        }
      }
    });
    //signalR
    this.connection.start().then(function() {
      
    }).catch(function (err) {
      return console.error(err.toString());
    });
    this.connection.on('UpdateBoard', function(x, y, color) {
      let piece = app.pieceArray[y][x];
      piece.classList.add('active');
      piece.classList.replace(app.faction, color);
      piece.removeEventListener('click', app.setPiece);
    });
  },
});