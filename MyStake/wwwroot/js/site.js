// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
//function sign() {
//    document.getElementById("myModal").style.display = "block";
//    document.getElementById("MyModal").style.display = "none";
//}
//function up() {
//    document.getElementById("myModal").style.display = "none";
//    document.querySelector(".modal-backdrop").style.display = "none";
//    document.getElementById("body").style.overflow = "auto";
//}

      function openNav() {
        document.getElementById("mySidenav").style.width = "280px";
          document.getElementById("spacesidenav").style.display = "flex";
      }
      window.onclick = function (event) {
        if (event.target == document.getElementById("spacesidenav")) {
          document.getElementById("spacesidenav").style.display = "none";
          document.getElementById("mySidenav").style.width = "0px";
          }
          if (event.target == document.getElementById("country-box")) {
              document.getElementById("country").style.transform = "translateY(90%)";
              document.getElementById("country").style.zIndex = "2"; 
              document.getElementById("country-box").style.visibility = "hidden";
          }
          if (event.target == document.getElementById("currency-box")) {
              document.getElementById("currency").style.transform = "translateY(90%)";
              document.getElementById("currency").style.zIndex = "2"; 
              document.getElementById("currency-box").style.visibility = "hidden";
          }
      };

      function openCity(evt, povidercode) {
        var i, tablinks;
        tablinks = document.getElementsByClassName("tablinks");
        for (i = 0; i < tablinks.length; i++) {
          tablinks[i].className = tablinks[i].className.replace(" active", "");
        }
          evt.currentTarget.className += " active";

      }

      class Cardsliders {
        constructor(section) {
          this.section = section;
          this.wrapper = section.querySelector(".sliders-wrapper");
          this.sliders = section.querySelector(".sliders");
          this.cards = section.querySelectorAll(".cardo");
          this.prevBtn = section.querySelector(".prev-btn");
          this.nextBtn = section.querySelector(".next-btn");

          this.isDragging = false;
          this.startX = 0;
          this.currentTranslate = 0;
          this.prevTranslate = 0;
          this.currentIndex = 0;

          this.visibleCards = parseInt(section.dataset.visible) || 3;
          this.gap = parseInt(section.dataset.gap) || 16;
          this.btnvisible = section.dataset.btnvisible;
          this.desktopCards = parseInt(section.dataset.desktop) || 3;
          this.tabletCards = parseInt(section.dataset.tablet) || 3;
          this.mobileCards = parseInt(section.dataset.mobile) || 2;
            this.smallMobileCards = parseInt(section.dataset.smallmobile) || 3
          ;
          this.cardWidth = 0;
          this.autoSlideDelay = 2500;
          this.autoSlide = null;

          this.init();
        }

        init() {
          this.setCardWidth();
          window.addEventListener("resize", () => this.setCardWidth());

          /* DRAG */
          this.wrapper.addEventListener("mousedown", (e) =>
            this.dragStart(e.clientX),
          );
          window.addEventListener("mousemove", (e) => this.dragMove(e.clientX));
          window.addEventListener("mouseup", () => this.dragEnd());

          /* TOUCH */
          this.wrapper.addEventListener("touchstart", (e) =>
            this.dragStart(e.touches[0].clientX),
          );
          window.addEventListener("touchmove", (e) =>
            this.dragMove(e.touches[0].clientX),
          );
          window.addEventListener("touchend", () => this.dragEnd());

          /* BUTTONS */
          if (this.prevBtn) {
            this.prevBtn.addEventListener("click", () => this.prev());
          }

          if (this.nextBtn) {
            this.nextBtn.addEventListener("click", () => this.next());
          }

          /* HOVER */
          this.wrapper.addEventListener("mouseenter", () =>
            this.stopAutoSlide(),
          );
          this.wrapper.addEventListener("mouseleave", () =>
            this.startAutoSlide(),
          );

          this.startAutoSlide();
        }

        setCardWidth() {
          const width = this.wrapper.offsetWidth;

          this.cardWidth =
            (width - this.gap * (this.visibleCards - 1)) / this.visibleCards;

          this.cards.forEach((cardo) => {
            cardo.style.width = `${this.cardWidth}px`;
          });

          this.goTo(this.currentIndex, false);
        }
        getVisibleCards() {
          const width = window.innerWidth;

          if (width <= 376) {
            return this.smallMobileCards;
          } else if (width <= 668) {
            return this.mobileCards;
          } else if (width <= 1024) {
              return this.smallMobileCards;
          } else if (width <= 1324) {
              return 5;
          } else {
            return this.desktopCards;
          }
        }

        setCardWidth() {
          this.visibleCards = this.getVisibleCards();

          const width = this.wrapper.offsetWidth;

          this.cardWidth =
            (width - this.gap * (this.visibleCards - 1)) / this.visibleCards;

          this.cards.forEach((card) => {
            card.style.width = `${this.cardWidth}px`;
          });

          /* FIX INDEX LIMIT */
          const maxIndex = Math.max(0, this.cards.length - this.visibleCards);

          if (this.currentIndex > maxIndex) {
            this.currentIndex = maxIndex;
          }

          this.goTo(this.currentIndex, false);
        }

        dragStart(x) {
          this.isDragging = true;

          this.startX = x;

          this.wrapper.classList.add("dragging");

          this.sliders.style.transition = "";

          this.stopAutoSlide();
        }

        dragMove(x) {
          if (!this.isDragging) return;

          const delta = x - this.startX;

          this.currentTranslate = this.prevTranslate + delta;

          this.updatesliders();
        }

        dragEnd() {
          if (!this.isDragging) return;

          this.isDragging = false;

          this.wrapper.classList.remove("dragging");

          const move = this.cardWidth + this.gap;

          let index = Math.round(-this.currentTranslate / move);

          this.currentIndex = this.limitIndex(index);

          this.goTo(this.currentIndex);

          this.startAutoSlide();
        }

        updatesliders() {
          this.sliders.style.transform = `translateX(${this.currentTranslate}px)`;
        }

        goTo(index, animate = true) {
          const move = this.cardWidth + this.gap;

          this.currentTranslate = -index * move;

          this.prevTranslate = this.currentTranslate;

          this.sliders.style.transition = animate ? "transform .4s ease" : "";

          this.updatesliders();
        }

        limitIndex(index) {
          const max = this.cards.length - this.visibleCards;

          return Math.max(0, Math.min(index, max));
        }

        next() {
          this.currentIndex++;

          if (this.currentIndex > this.cards.length - this.visibleCards) {
            this.currentIndex = 0;
          }

          this.goTo(this.currentIndex);
        }

        prev() {
          this.currentIndex--;

          if (this.currentIndex < 0) {
            this.currentIndex = this.cards.length - this.visibleCards;
          }

          this.goTo(this.currentIndex);
        }

        startAutoSlide() {
          this.stopAutoSlide();

          this.autoSlide = setInterval(() => {
            this.next();
          }, this.autoSlideDelay);
        }

        stopAutoSlide() {
          clearInterval(this.autoSlide);
        }
      }

      /* INIT ALL slidersS */
      document.querySelectorAll(".sliders-section").forEach((section) => {
        new Cardsliders(section);
      });
      
const DROPdowns = document.querySelectorAll(".login-country-dropdown");

DROPdowns.forEach((DROPdown) => {
    const selected = DROPdown.querySelector(".login-country-dropdown-selected");

    const menu = DROPdown.querySelector(".login-country-dropdown-menu");

    const items = DROPdown.querySelectorAll(".login-country-dropdown-item");

    const selectedText = DROPdown.querySelector(".selected-text");

    // OPEN DROPDOWN
    selected.addEventListener("click", () => {
        // CLOSE OTHER DROPDOWNS
        if (window.innerWidth > 980) {
            DROPdowns.forEach((otherDROPdown) => {
                if (otherDROPdown !== DROPdown) {
                    otherDROPdown.querySelector(".login-country-dropdown-menu").classList.remove("show");
                    otherDROPdown
                        .querySelector(".login-country-dropdown-selected")
                        .classList.remove("active");
                }
            });

            menu.classList.toggle("show");

            selected.classList.toggle("active");
        }
        else {

            document.getElementById("country").style.transform = "translateY(5%)";
            document.getElementById("country").style.zIndex = "2000";
            document.getElementById("country-box").style.visibility = "visible";
            document.getElementById("country-box").style.display = "flex";
            document.getElementById("country").style.display = "flex";

        }
    });

    // SELECT ITEM
    items.forEach((item) => {
        item.addEventListener("click", () => {
            selectedText.textContent = item.dataset.value;

            menu.classList.remove("show");

            selected.classList.remove("active");
        });
    });
});

// CLOSE ON OUTSIDE CLICK
document.addEventListener("click", (e) => {
    if (!e.target.closest(".login-country-dropdown")) {
        DROPdowns.forEach((DROPdown) => {
            DROPdown.querySelector(".login-country-dropdown-menu").classList.remove("show");
            DROPdown.querySelector(".login-country-dropdown-selected").classList.remove("active");
            document.getElementById("country").style.transform = "translateY(90%)";
            document.getElementById("country").style.zIndex = "2";
            document.getElementById("country-box").style.visibility = "hidden";
        });
    }
});

const EDROPdowns = document.querySelectorAll(".currency-dropdown");

EDROPdowns.forEach((EDROPdown) => {
    const Eselected = EDROPdown.querySelector(".currency-dropdown-selected");

    const Emenu = EDROPdown.querySelector(".currency-dropdown-menu");

    const Eitems = EDROPdown.querySelectorAll(".currency-dropdown-item");

    const EselectedText = EDROPdown.querySelector(".Eselected-text");

    // OPEN DROPDOWN
    Eselected.addEventListener("click", () => {
        // CLOSE OTHER DROPDOWNS
        if (window.innerWidth > 980) {
            EDROPdowns.forEach((otherEDROPdown) => {
                if (otherEDROPdown !== EDROPdown) {
                    otherEDROPdown.querySelector(".currency-dropdown-menu").classList.remove("show");
                    otherEDROPdown
                        .querySelector(".currency-dropdown-selected")
                        .classList.remove("active");
                }
            });

            Emenu.classList.toggle("show");

            Eselected.classList.toggle("active");
        }
        else {

            document.getElementById("currency").style.transform = "translateY(5%)";
            document.getElementById("currency").style.zIndex = "2000";
            document.getElementById("currency-box").style.visibility = "visible";
            document.getElementById("currency-box").style.display = "flex";
            document.getElementById("currency").style.display = "flex";
        }
    });

    // SELECT ITEM
    Eitems.forEach((Eitem) => {
        Eitem.addEventListener("click", () => {
            EselectedText.textContent = Eitem.dataset.value;

            Emenu.classList.remove("show");

            Eselected.classList.remove("active");
        });
    });
});

// CLOSE ON OUTSIDE CLICK
document.addEventListener("click", (e) => {
    if (!e.target.closest(".currency-dropdown")) {
        EDROPdowns.forEach((EDROPdown) => {
            EDROPdown.querySelector(".currency-dropdown-menu").classList.remove("show");
            EDROPdown.querySelector(".currency-dropdown-selected").classList.remove("active");
            document.getElementById("currency").style.transform = "translateY(90%)";
            document.getElementById("currency").style.zIndex = "2";
            document.getElementById("currency-box").style.visibility = "hidden";
        });
    }
});

const Dropdowns = document.querySelectorAll(".country-dropdown");

Dropdowns.forEach((Dropdown) => {
    const selecteD = Dropdown.querySelector(".country-dropdown-selected");

    const menU = Dropdown.querySelector(".country-dropdown-menu");

    const itemS = Dropdown.querySelectorAll(".country-dropdown-item");

    const selectedTexT = Dropdown.querySelector(".selected-texT");

    // OPEN DropDOWN
    selecteD.addEventListener("click", () => {
        // CLOSE OTHER DropDOWNS
       //    Dropdowns.forEach((otherDropdown) => {
       //        if (otherDropdown !== Dropdown) {
       //
       //         console.log(otherDropdown.querySelector(".Dropdown-menu"))
       //        otherDropdown
        //         .querySelector(".Dropdown-menu")
         //        .classList.remove("show");
      //  
       //        otherDropdown
       //          .querySelector(".Dropdown-selected")
       //          .classList.remove("active");
       //     }
       //    });

        menU.classList.toggle("show");

        selecteD.classList.toggle("active");
    });

    // SELECT ITEM
    itemS.forEach((item) => {
        item.addEventListener("click", () => {
            selectedTexT.textContent = item.dataset.value;

            menU.classList.remove("show");

            selecteD.classList.remove("active");
        });
    });
});

// CLOSE ON OUTSIDE CLICK
document.addEventListener("click", (e) => {
    if (!e.target.closest(".country-dropdown")) {
        Dropdowns.forEach((Dropdown) => {
            Dropdown.querySelector(".country-dropdown-menu").classList.remove("show");

            Dropdown.querySelector(".country-dropdown-selected").classList.remove(
                "active",
            );

        });
    }
});
      const Selected = document.getElementById("DRopdownSelected");
      const Menu = document.getElementById("DRopdownMenu");
      const SelectedText = document.getElementById("selectedText");
      const Items = document.querySelectorAll(".DRopdown-item");

      // OPEN / CLOSE
      //Selected.addEventListener("click", () => {
      //  Menu.classList.toggle("show");
      //  Selected.classList.toggle("active");
      //});

      // SELECT ITEM
      //Items.forEach((item) => {
      //  item.addEventListener("click", () => {
      //    SelectedText.innerHTML = `${item}`;

      //    Menu.classList.remove("show");
      //    Selected.classList.remove("active");
      //  });
      //});

      // CLOSE WHEN CLICK OUTSIDE
      //document.addEventListener("click", (e) => {
      //  if (!e.target.closest(".DRopdown")) {
      //    Menu.classList.remove("show");
      //    Selected.classList.remove("active");
      //  }    });


//const DROpdown = document.getElementById("flag-dropdown");
//      const selectedFlag = document.getElementById("selectedFlag");
//      const selectedBtn = document.getElementById("selectedBtn");

//      selectedBtn.addEventListener("click", () => {
//        DROpdown.classList.toggle("active");
//      });

//      document.querySelectorAll(".option").forEach((option) => {
//        option.addEventListener("click", () => {
//          selectedFlag.src = option.dataset.flag;
//          DROpdown.classList.remove("active");
//        });
//      });

//      document.addEventListener("click", (e) => {
//        if (!DROpdown.contains(e.target)) {
//          DROpdown.classList.remove("active");
//        }
//      });


//const DROPDOWN = document.getElementById("pc-country-dropdown");
//const SELECTEDFlag = document.getElementById("SELECTEDFlag");
//const SELECTEDBtn = document.getElementById("SELECTEDBtn");
//const SELECTEDTEXT = document.getElementById("SELECTEDTEXT");
//SELECTEDBtn.addEventListener("click", () => {
//    DROPDOWN.classList.toggle("active");
//});

//document.querySelectorAll(".OPtion").forEach((OPtion) => {
//    OPtion.addEventListener("click", () => {
//        SELECTEDFlag.src = OPtion.dataset.flag;
//        DROPDOWN.classList.remove("active");
//        SELECTEDTEXT.textContent = OPtion.dataset.value;
//    });
//});

//document.addEventListener("click", (e) => {
//    if (!DROPDOWN.contains(e.target)) {
//        DROPDOWN.classList.remove("active");
//    }
//});

$(".SLIDER").slick({
    dots: true,
    arrows: true,
    infinite: true,
    speed: 500,
    slidesToShow: 1,
    autoplay: true,
    autoplaySpeed: 2000,
});
$(".sLIDER").slick({
    dots: true,
    arrows: true,
    infinite: true,
    speed: 500,
    slidesToShow: 1,
    autoplay: true,
    autoplaySpeed: 2000,
});



function no(o) {
    document.getElementById("country").style.transform = "translateY(90%)";
    document.getElementById("country").style.zIndex = "2";
    document.getElementById("country-box").style.visibility = "hidden";
    const countrys = document.getElementsByClassName("country-item");
    const ff = countrys[o - 1].dataset.value; 
    document.getElementById("myBtn").innerHTML = `${ff}`; 

}
function No(o) {
    document.getElementById("currency").style.transform = "translateY(90%)";
    document.getElementById("currency").style.zIndex = "2";
    document.getElementById("currency-box").style.visibility = "hidden";
    const currencys = document.getElementsByClassName("currency-item");
    document.getElementById("mybtn").innerHTML = `${o}`; 

}
const Modal = document.getElementById("Modal");
const iframe = document.getElementById("ModalIframe");
const closeBtn = document.getElementById("closeModal");

closeBtn.addEventListener("click", function () {
    Modal.classList.remove("show");

    document.body.style.overflow = "";

    iframe.src = "";
});


    